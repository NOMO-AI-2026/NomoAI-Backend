using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Common.Jwt
{
    public sealed record IssuedRefreshToken(
        string RawToken,
        DateTime ExpiresAtUtc);

    public sealed record RefreshTokenRotationResult(
        ApplicationUser User,
        string RawRefreshToken,
        DateTime RefreshTokenExpiresAtUtc);

    public interface IRefreshTokenService
    {
        Task<Result<IssuedRefreshToken>> IssueAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default);

        Task<Result<RefreshTokenRotationResult>> RotateAsync(
            string rawIncoming,
            CancellationToken cancellationToken = default);

        Task RevokeAsync(
            string rawIncoming,
            CancellationToken cancellationToken = default);

        Task RevokeAllForUserAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }
}
