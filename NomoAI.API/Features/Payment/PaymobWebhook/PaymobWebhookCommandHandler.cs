using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Payment.PaymobWebhook
{
    internal sealed class PaymobWebhookCommandHandler : IRequestHandler<PaymobWebhookCommand, Result>
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymobWebhookCommandHandler> _logger;

        public PaymobWebhookCommandHandler(
            AppDbContext db,
            IConfiguration config,
            ILogger<PaymobWebhookCommandHandler> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task<Result> Handle(PaymobWebhookCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Hmac))
            {
                return Result.Failure(PaymentErrors.InvalidHmac);
            }

            if (request.Payload?.Obj is null)
            {
                return Result.Failure(PaymentErrors.InvalidCallback);
            }

            var hmacKey = _config["Paymob:HmacKey"] ?? _config["PayMob:HmacKey"];
            if (string.IsNullOrWhiteSpace(hmacKey))
            {
                _logger.LogError("Paymob:HmacKey is not configured.");
                return Result.Failure(PaymentErrors.InvalidHmac);
            }

            if (!PaymobHmacValidator.IsValid(request.Payload.Obj, request.Hmac, hmacKey))
            {
                _logger.LogWarning("PayMob webhook rejected due to invalid HMAC.");
                return Result.Failure(PaymentErrors.InvalidHmac);
            }

            var obj = request.Payload.Obj;
            var referenceId = ResolveReferenceId(obj);
            if (string.IsNullOrWhiteSpace(referenceId))
            {
                _logger.LogWarning(
                    "PayMob webhook missing merchant reference. TransactionId={TransactionId}, PayloadType={Type}",
                    obj.Id,
                    request.Payload.Type);
                return Result.Failure(PaymentErrors.InvalidCallback);
            }

            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var payment = await _db.Payments
                    .FirstOrDefaultAsync(
                        p => p.ReferenceId == referenceId && !p.IsDeleted,
                        cancellationToken);

                if (payment is null)
                {
                    _logger.LogWarning("PayMob webhook payment not found. ReferenceId={ReferenceId}", referenceId);
                    await tx.RollbackAsync(cancellationToken);
                    return Result.Failure(PaymentErrors.PaymentNotFound);
                }

                if (payment.Status is PaymentStatus.Paid)
                {
                    await tx.CommitAsync(cancellationToken);
                    return Result.Success();
                }

                var purchaseExists = await _db.DoctorPlanPurchases
                    .AsNoTracking()
                    .AnyAsync(p => p.PaymentId == payment.Id && !p.IsDeleted, cancellationToken);

                if (purchaseExists)
                {
                    if (payment.Status != PaymentStatus.Paid)
                    {
                        payment.Status = PaymentStatus.Paid;
                        payment.PaidAtUtc ??= DateTime.UtcNow;
                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    await tx.CommitAsync(cancellationToken);
                    return Result.Success();
                }

                if (!obj.Success || obj.ErrorOccured || obj.Pending)
                {
                    payment.Status = PaymentStatus.Failed;
                    await _db.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                    return Result.Success();
                }

                if (!TryParsePlanId(referenceId, out var planId))
                {
                    _logger.LogWarning("Could not parse plan id from ReferenceId={ReferenceId}", referenceId);
                    await tx.RollbackAsync(cancellationToken);
                    return Result.Failure(PaymentErrors.InvalidCallback);
                }

                var plan = await _db.SupscriptionPlan
                    .AsNoTracking()
                    .Where(p => p.Id == planId && !p.IsDeleted)
                    .Select(p => new { p.Id, p.IncludedMinutes, p.Price })
                    .FirstOrDefaultAsync(cancellationToken);

                if (plan is null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return Result.Failure(PaymentErrors.PlanNotFound);
                }

                var purchasedMinutes = plan.IncludedMinutes;
                var now = DateTime.UtcNow;

                payment.Status = PaymentStatus.Paid;
                payment.PaidAtUtc = now;

                var purchase = new DoctorPlanPurchase
                {
                    DoctorId = payment.DoctorId,
                    PlanId = plan.Id,
                    PaymentId = payment.Id,
                    PurchasedMinutes = purchasedMinutes,
                    PurchasedPrice = payment.Amount,
                    PurchasedAtUtc = now
                };

                _db.DoctorPlanPurchases.Add(purchase);

                var wallet = await _db.DoctorCreditWallets
                    .FirstOrDefaultAsync(
                        w => w.DoctorId == payment.DoctorId && !w.IsDeleted,
                        cancellationToken);

                if (wallet is not null)
                {
                    wallet.AvailableMinutes += purchasedMinutes;
                    wallet.UpdatedAtUtc = now;
                }
                else
                {
                    wallet = new DoctorCreditWallet
                    {
                        DoctorId = payment.DoctorId,
                        AvailableMinutes = purchasedMinutes,
                        UpdatedAtUtc = now
                    };
                    _db.DoctorCreditWallets.Add(wallet);
                }

                await _db.SaveChangesAsync(cancellationToken);

                _db.DoctorTransactions.Add(new DoctorTransaction
                {
                    DoctorId = payment.DoctorId,
                    Type = TransactionType.PlanPurchase,
                    Minutes = purchasedMinutes,
                    BalanceAfter = wallet.AvailableMinutes,
                    PlanPurchaseId = purchase.Id
                });

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "PayMob payment credited. PaymentId={PaymentId}, DoctorId={DoctorId}, Minutes={Minutes}",
                    payment.Id,
                    payment.DoctorId,
                    purchasedMinutes);

                return Result.Success();
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static string? ResolveReferenceId(PaymobTransactionObjDto obj)
        {
            if (!string.IsNullOrWhiteSpace(obj.Order?.MerchantOrderId))
            {
                return obj.Order.MerchantOrderId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(obj.MerchantOrderId))
            {
                return obj.MerchantOrderId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(obj.Order?.MerchantOrder))
            {
                return obj.Order.MerchantOrder.Trim();
            }

            return null;
        }

        /// <summary>
        /// Reference format: NOM-{doctorId}-{planId}-{guid32hex}
        /// </summary>
        private static bool TryParsePlanId(string referenceId, out int planId)
        {
            planId = 0;
            var parts = referenceId.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || !parts[0].Equals("NOM", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return int.TryParse(parts[2], out planId) && planId > 0;
        }
    }
}
