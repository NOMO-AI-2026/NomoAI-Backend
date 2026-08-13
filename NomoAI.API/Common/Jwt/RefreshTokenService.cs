using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Auth;
using NomoAI.API.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace NomoAI.API.Common.Jwt
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _config;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            AppDbContext dbContext,
            IConfiguration config,
            ILogger<RefreshTokenService> logger)
        {
            _dbContext = dbContext;
            _config = config;
            _logger = logger;
        }

        public async Task<Result<IssuedRefreshToken>> IssueAsync(
            ApplicationUser user,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var (rawToken, entity) = CreateToken(user.Id);
                _dbContext.RefreshTokens.Add(entity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(
                    new IssuedRefreshToken(rawToken, entity.ExpiresAtUtc));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to persist a refresh token for user {UserId}.",
                    user.Id);

                return Result.Failure<IssuedRefreshToken>(
                    AuthErrors.RefreshTokenIssueFailed);
            }
        }

        public async Task<Result<RefreshTokenRotationResult>> RotateAsync(
            string rawIncoming,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawIncoming))
            {
                return Result.Failure<RefreshTokenRotationResult>(
                    AuthErrors.InvalidRefreshToken);
            }

            string incomingHash = HashToken(rawIncoming.Trim());

            RefreshToken? existing = await _dbContext.RefreshTokens
                .Include(token => token.User)
                .FirstOrDefaultAsync(
                    token => token.TokenHash == incomingHash,
                    cancellationToken);

            if (existing is null)
            {
                return Result.Failure<RefreshTokenRotationResult>(
                    AuthErrors.InvalidRefreshToken);
            }

            if (existing.RevokedAtUtc is not null)
            {
                await RevokeAllForUserAsync(existing.UserId, cancellationToken);

                return Result.Failure<RefreshTokenRotationResult>(
                    AuthErrors.RefreshTokenReused);
            }

            if (existing.ExpiresAtUtc <= DateTime.UtcNow)
            {
                return Result.Failure<RefreshTokenRotationResult>(
                    AuthErrors.RefreshTokenExpired);
            }

            if (existing.User is null || existing.User.IsDeleted)
            {
                return Result.Failure<RefreshTokenRotationResult>(
                    AuthErrors.InvalidRefreshToken);
            }

            try
            {
                var (rawNew, newEntity) = CreateToken(existing.UserId);

                existing.RevokedAtUtc = DateTime.UtcNow;
                existing.ReplacedByTokenHash = newEntity.TokenHash;
                _dbContext.RefreshTokens.Add(newEntity);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return Result.Success(
                    new RefreshTokenRotationResult(
                        existing.User,
                        rawNew,
                        newEntity.ExpiresAtUtc));
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to rotate the refresh token for user {UserId}.",
                    existing.UserId);

                return Result.Failure<RefreshTokenRotationResult>(
                    AuthErrors.RefreshTokenIssueFailed);
            }
        }

        public async Task RevokeAsync(
            string rawIncoming,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawIncoming))
            {
                return;
            }

            string tokenHash = HashToken(rawIncoming.Trim());
            DateTime revokedAtUtc = DateTime.UtcNow;

            await _dbContext.RefreshTokens
                .Where(token =>
                    token.TokenHash == tokenHash &&
                    token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(
                        token => token.RevokedAtUtc,
                        revokedAtUtc),
                    cancellationToken);
        }

        public async Task RevokeAllForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            DateTime revokedAtUtc = DateTime.UtcNow;

            await _dbContext.RefreshTokens
                .Where(token =>
                    token.UserId == userId &&
                    token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(
                        token => token.RevokedAtUtc,
                        revokedAtUtc),
                    cancellationToken);
        }

        private (string RawToken, RefreshToken Entity) CreateToken(string userId)
        {
            string rawToken = GenerateRawToken();
            DateTime createdAtUtc = DateTime.UtcNow;

            var entity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = HashToken(rawToken),
                CreatedAtUtc = createdAtUtc,
                ExpiresAtUtc = createdAtUtc.AddDays(GetRefreshTokenDays())
            };

            return (rawToken, entity);
        }

        private int GetRefreshTokenDays()
        {
            int refreshTokenDays = _config.GetValue("Jwt:RefreshTokenDays", 14);
            return refreshTokenDays > 0 ? refreshTokenDays : 14;
        }

        private static string GenerateRawToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            return Base64UrlEncoder.Encode(randomBytes);
        }

        private static string HashToken(string rawToken)
        {
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(hashBytes);
        }
    }
}
