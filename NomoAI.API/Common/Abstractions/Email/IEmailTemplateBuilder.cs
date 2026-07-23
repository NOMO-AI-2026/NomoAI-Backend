using NomoAI.API.Common.Email;
using NomoAI.API.Common.Enums;

namespace NomoAI.API.Common.Abstractions.Email;

public interface IEmailTemplateBuilder
{
    EmailMessage BuildOtpMessage(
        EmailOtpPurpose purpose,
        string otp,
        int expirationMinutes);

    EmailMessage BuildEmailChangedNotification(
        string newEmail);
}
