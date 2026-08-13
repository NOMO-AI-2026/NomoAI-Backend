using Microsoft.EntityFrameworkCore;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Persistence;

namespace NomoAI.API.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Hangfire recurring job: cancels Pending payments whose quick link has expired.
    /// Visible under Recurring Jobs in the Hangfire dashboard (/jobs).
    /// </summary>
    public sealed class ExpirePaymentQuickLinksBackgroundJob
    {
        public const string RecurringJobId = "expire-payment-quick-links";

        private readonly AppDbContext _db;
        private readonly ILogger<ExpirePaymentQuickLinksBackgroundJob> _logger;

        public ExpirePaymentQuickLinksBackgroundJob(
            AppDbContext db,
            ILogger<ExpirePaymentQuickLinksBackgroundJob> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            var now = DateTime.UtcNow;

            int affected = await _db.Payments
                .Where(payment =>
                    !payment.IsDeleted &&
                    payment.Status == PaymentStatus.Pending &&
                    payment.PaymentQuickLinks.Any(link =>
                        !link.IsDeleted &&
                        link.ExpiresAt != null &&
                        link.ExpiresAt <= now))
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    payment => payment.Status,
                    PaymentStatus.Cancelled));

            if (affected > 0)
            {
                _logger.LogInformation(
                    "Cancelled {Count} pending payment(s) with expired quick links.",
                    affected);
            }
            else
            {
                _logger.LogInformation(
                    "ExpirePaymentQuickLinks: no expired pending payments found.");
            }
        }
    }
}
