using NomoAI.API.Common.Email;
using NomoAI.API.Common.Enums;

namespace NomoAI.API.Common.Abstractions.Email;

public interface IEmailOtpDispatcher
{
    Task<Result<EmailOtpDispatchResult>> SendAsync(
        string userId,
        string targetEmail,
        EmailOtpPurpose purpose,
        CancellationToken cancellationToken = default);
}
