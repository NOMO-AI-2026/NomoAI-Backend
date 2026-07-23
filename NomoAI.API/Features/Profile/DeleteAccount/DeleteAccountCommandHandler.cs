using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Roles;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Profile.DeleteAccount
{
    internal sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, Result>
    {
        private readonly AppDbContext _db;
        private readonly IRoleManger _roleManger;

        public DeleteAccountCommandHandler(AppDbContext db, IRoleManger roleManger)
        {
            _db = db;
            _roleManger = roleManger;
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

            return Result.Success();
        }
    }
}
