using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;
using NomoAI.API.Common.Roles;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Profile.DeleteAccount
{
    internal sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, Result>
    {
        private readonly AppDbContext _db;
        private readonly IRoleManger _roleManger;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<DeleteAccountCommandHandler> _logger;

        public DeleteAccountCommandHandler(
            AppDbContext db,
            IRoleManger roleManger,
            IRefreshTokenService refreshTokenService,
            ILogger<DeleteAccountCommandHandler> logger)
        {
            _db = db;
            _roleManger = roleManger;
            _refreshTokenService = refreshTokenService;
            _logger = logger;
        }

        public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Where(u => u.Id == request.UserId && !u.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure(Profile.ProfileErrors.UserNotFound);
            }

            user.IsDeleted = true;

            if (!string.IsNullOrWhiteSpace(request.Role) && request.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            {
                var doctor = await _db.Doctor
                    .Where(d => d.UserId == request.UserId && !d.IsDeleted)
                    .SingleOrDefaultAsync(cancellationToken);
                if (doctor is not null)
                {
                    doctor.IsDeleted = true;
                }
            }
            else
            {
                var parent = await _db.Parents
                    .Where(p => p.UserId == request.UserId && !p.IsDeleted)
                    .SingleOrDefaultAsync(cancellationToken);
                if (parent is not null)
                {
                    parent.IsDeleted = true;
                }
            }
            await _roleManger.DeleteRolesFromUser(user);
            await _db.SaveChangesAsync(cancellationToken);

            await TryRevokeAllRefreshTokensAsync(
                user.Id,
                cancellationToken);

            return Result.Success();
        }

        private async Task TryRevokeAllRefreshTokensAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            try
            {
                await _refreshTokenService.RevokeAllForUserAsync(
                    userId,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Account was deleted, but refresh tokens could not " +
                    "be revoked for user {UserId}.",
                    userId);
            }
        }
    }
}
