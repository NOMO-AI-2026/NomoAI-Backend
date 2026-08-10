using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Abstractions.Email;
using NomoAI.API.Common.Email;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.AdminDashboard.ToggleDoctorApproval
{
    internal sealed class ToggleDoctorApprovalCommandHandler : IRequestHandler<ToggleDoctorApprovalCommand, Result>
    {
        private readonly AppDbContext _db;
        private readonly IEmailTemplateBuilder _emailTemplateBuilder;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ToggleDoctorApprovalCommandHandler> _logger;

        public ToggleDoctorApprovalCommandHandler(
            AppDbContext db,
            IEmailTemplateBuilder emailTemplateBuilder,
            IEmailSender emailSender,
            ILogger<ToggleDoctorApprovalCommandHandler> logger)
        {
            _db = db;
            _emailTemplateBuilder = emailTemplateBuilder;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task<Result> Handle(ToggleDoctorApprovalCommand request, CancellationToken cancellationToken)
        {
            var doctor = await _db.Doctor
                .Include(d => d.User)
                .Where(d => d.UserId == request.UserId && !d.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (doctor is null)
            {
                return Result.Failure(AdminDashboardErrors.DoctorNotFound);
            }

            bool wasApproved = doctor.IsApproved;

            doctor.IsApproved = request.ApproveStatus;
            await _db.SaveChangesAsync(cancellationToken);

            if (request.ApproveStatus && !wasApproved)
            {
                await TrySendDoctorApprovedNotificationAsync(
                    doctor.UserId,
                    doctor.User?.Email,
                    doctor.User?.Fullname,
                    cancellationToken);
            }

            return Result.Success();
        }

        private async Task TrySendDoctorApprovedNotificationAsync(
            string userId,
            string? email,
            string? displayName,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning(
                    "Doctor approval email was not sent because " +
                    "user {UserId} has no email address.",
                    userId);
                return;
            }

            EmailMessage message =
                _emailTemplateBuilder.BuildDoctorApprovedNotification(
                    displayName);

            try
            {
                await _emailSender.SendAsync(
                    email,
                    message.Subject,
                    message.HtmlBody,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Doctor approval notification could not be sent " +
                    "for user {UserId}.",
                    userId);
            }
        }
    }
}
