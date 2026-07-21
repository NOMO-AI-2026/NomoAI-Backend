using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Children.DeleteChildren
{
    internal sealed class DeleteChildrenCommandHandler : IRequestHandler<DeleteChildrenCommand, Result<bool>>
    {
        private readonly AppDbContext _db;
        public DeleteChildrenCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<bool>> Handle(DeleteChildrenCommand request, CancellationToken cancellationToken)
        {
            var child = await _db.Children
                .Where(c => c.Id == request.ChildId && !c.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (child is null)
            {
                return Result.Failure<bool>(new Error("Children.ChildNotFound", "Child not found.", 404));
            }

            child.IsDeleted = true;
            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success(true);
        }
    }
}
