using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.AdminDashboard.DeleteParent
{
    internal sealed class DeleteParentCommandHandler : IRequestHandler<DeleteParentCommand, Result>
    {
        private readonly AppDbContext _db;
        public DeleteParentCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(DeleteParentCommand request, CancellationToken cancellationToken)
        {
            var user = await _db.Users
                .Where(u => u.Id == request.UserId && !u.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (user is null)
            {
                return Result.Failure(AdminDashboardErrors.ParentNotFound);
            }

            user.IsDeleted = true;

            var parent = await _db.Parents
                .Where(p => p.UserId == request.UserId && !p.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (parent is not null)
            {
                parent.IsDeleted = true;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
