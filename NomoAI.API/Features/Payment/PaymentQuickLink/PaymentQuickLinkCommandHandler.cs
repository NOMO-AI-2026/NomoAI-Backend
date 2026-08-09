using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Infrastructure.PayMob.Models;
using NomoAI.API.Infrastructure.PayMob.Services;
using NomoAI.API.Persistence;
using System.ComponentModel;

namespace NomoAI.API.Features.Payment.PaymentQuickLink
{
    internal sealed class PaymentQuickLinkCommandHandler
        : IRequestHandler<PaymentQuickLinkCommand, Result<PaymentQuickLinkResponse>>
    {
        private readonly AppDbContext _db;
        private readonly IPayMobService _payMobService;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentQuickLinkCommandHandler> _logger;

        public PaymentQuickLinkCommandHandler(
            AppDbContext db,
            IPayMobService payMobService,
            IConfiguration config,
            ILogger<PaymentQuickLinkCommandHandler> logger)
        {
            _db = db;
            _payMobService = payMobService;
            _config = config;
            _logger = logger;
        }

        public async Task<Result<PaymentQuickLinkResponse>> Handle(
            PaymentQuickLinkCommand request,
            CancellationToken cancellationToken)
        {
            var idempotency = request.Idempotency.Trim();

            var existingLink = await _db.PaymentQuickLinks
                .AsNoTracking()
                .Where(l => l.Idempotency == idempotency && !l.IsDeleted)
                .Select(l => new
                {
                    l.PaymentId,
                    l.clientUrl,
                    l.shortUrl,
                    l.ExpiresAt,
                    ReferenceId = l.Payment.ReferenceId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingLink is not null)
            {
                return Result.Success(new PaymentQuickLinkResponse(
                    existingLink.PaymentId,
                    existingLink.clientUrl,
                    existingLink.shortUrl,
                    existingLink.ReferenceId,
                    existingLink.ExpiresAt,
                    IsReplay: true));
            }

            var doctor = await _db.Doctor
                .AsNoTracking()
                .Where(d => d.UserId == request.DoctorUserId && !d.IsDeleted)
                .Select(d => new
                {
                    Id =  d.Id,
                   FullName = d.User.Fullname,
                    Email = d.User.Email,
                    PhoneNumber = d.User.PhoneNumber
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (doctor is null)
            {
                return Result.Failure<PaymentQuickLinkResponse>(PaymentErrors.DoctorNotFound);
            }


            var plan = await _db.SupscriptionPlan
                .AsNoTracking()
                .Where(p => p.Id == request.PlanId && !p.IsDeleted)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Currency
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (plan is null)
            {
                return Result.Failure<PaymentQuickLinkResponse>(PaymentErrors.PlanNotFound);
            }

            var paymentMethod = await _db.PaymentMethods
                .AsNoTracking()
                .Where(m => m.Id == request.PaymentMethodId)
                .Select(m => new { m.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (paymentMethod is null)
            {
                return Result.Failure<PaymentQuickLinkResponse>(PaymentErrors.PaymentMethodNotFound);
            }

            var expiresHours = _config.GetValue("Paymob:LinkExpiresHours", 24);
            if (expiresHours <= 0)
            {
                expiresHours = 24;
            }

            DateTime expiresAt = DateTime.UtcNow.AddHours(expiresHours);
            string referenceId = $"NOM-{doctor.Id}-{plan.Id}-{Guid.NewGuid():N}";
            bool isLive = _config.GetValue("Paymob:IsLive", false);
            string amountCents = ((int)Math.Round(request.PriceInEGP * 100m, MidpointRounding.AwayFromZero))
                .ToString();

            CreateQuickLinkResponse payMobResponse;
            try
            {
                QuickLinkRequest QuickLinkRequest = new QuickLinkRequest
                {
                    AmountCents = amountCents,
                    ExpiresAt = expiresAt,
                    ReferenceId = referenceId,
                    PaymentMethods = [paymentMethod.Id],
                    Email = doctor.Email ?? string.Empty,
                    IsLive = isLive,
                    FullName = doctor.FullName ?? string.Empty,
                    PhoneNumber = doctor.PhoneNumber != null ? $"+2{doctor.PhoneNumber}": string.Empty,
                    Description = $"NomoAI plan: {plan.Name}"
                };
                Console.WriteLine($"QuickLinkRequest: AmountCents={QuickLinkRequest.AmountCents}, " +
                  $"ExpiresAt={QuickLinkRequest.ExpiresAt}, " +
                  $"ReferenceId={QuickLinkRequest.ReferenceId}, " +
                  $"PaymentMethods=[{string.Join(", ", QuickLinkRequest.PaymentMethods)}], " +
                  $"Email={QuickLinkRequest.Email}, " +
                  $"IsLive={QuickLinkRequest.IsLive}, " +
                  $"FullName={QuickLinkRequest.FullName}, " +
                  $"PhoneNumber=+2{QuickLinkRequest.PhoneNumber}, " +
                  $"Description={QuickLinkRequest.Description}");
                payMobResponse = await _payMobService.CreateQuickLinkAsync(QuickLinkRequest);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "PayMob quick link creation failed for doctor {DoctorId}", doctor.Id);
                return Result.Failure<PaymentQuickLinkResponse>(PaymentErrors.PayMobFailed);
            }

            var payment = new Domain.Entities.Payment
            {
                DoctorId = doctor.Id,
                PaymentMethodId = paymentMethod.Id,
                Amount = request.PriceInEGP,
                Currency = MoneyCurrency.EGP,
                Status = PaymentStatus.Pending,
                ReferenceId = referenceId,
                Provider = PaymentProvider.Paymob
            };

            var quickLink = new Domain.Entities.PaymentQuickLink
            {
                Payment = payment,
                Idempotency = idempotency,
                clientUrl = payMobResponse.ClientUrl,
                shortUrl = payMobResponse.ShortenUrl,
                ExpiresAt = payMobResponse.ExpiresAt ?? expiresAt
            };

            _db.Payments.Add(payment);
            _db.PaymentQuickLinks.Add(quickLink);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                var replay = await _db.PaymentQuickLinks
                    .AsNoTracking()
                    .Where(l => l.Idempotency == idempotency && !l.IsDeleted)
                    .Select(l => new
                    {
                        l.PaymentId,
                        l.clientUrl,
                        l.shortUrl,
                        l.ExpiresAt,
                        ReferenceId = l.Payment.ReferenceId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (replay is not null)
                {
                    return Result.Success(new PaymentQuickLinkResponse(
                        replay.PaymentId,
                        replay.clientUrl,
                        replay.shortUrl,
                        replay.ReferenceId,
                        replay.ExpiresAt,
                        IsReplay: true));
                }

                _logger.LogWarning(ex, "Idempotency conflict for key {Idempotency}", idempotency);
                return Result.Failure<PaymentQuickLinkResponse>(PaymentErrors.IdempotencyConflict);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to persist payment after PayMob success. ReferenceId={ReferenceId}, ClientUrl={ClientUrl}",
                    referenceId,
                    payMobResponse.ClientUrl);

                return Result.Success(new PaymentQuickLinkResponse(
                    0,
                    payMobResponse.ClientUrl,
                    payMobResponse.ShortenUrl,
                    referenceId,
                    payMobResponse.ExpiresAt ?? expiresAt));
            }

            return Result.Success(new PaymentQuickLinkResponse(
                payment.Id,
                quickLink.clientUrl,
                quickLink.shortUrl,
                payment.ReferenceId,
                quickLink.ExpiresAt));
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException?.Message.Contains("IX_", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
