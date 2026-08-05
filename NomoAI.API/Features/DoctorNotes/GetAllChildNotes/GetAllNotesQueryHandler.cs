using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.DoctorNotes.GetAllChildNotes
{
    internal sealed class GetAllNotesQueryHandler : IRequestHandler<GetAllNotesQuery, Result<PaginatedList<GetAllNotesResponse>>>
    {
        private readonly AppDbContext _db;
        public GetAllNotesQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<PaginatedList<GetAllNotesResponse>>> Handle(GetAllNotesQuery request, CancellationToken cancellationToken)
        {
            var childExists = await _db.Children
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.ChildId && !c.IsDeleted, cancellationToken);

            if (!childExists)
            {
                return Result.Failure<PaginatedList<GetAllNotesResponse>>(DoctorNotesErrors.ChildNotFound);
            }

            var query = _db.DoctorNotes
                .AsNoTracking()
                .Where(n => !n.IsDeleted && n.ChildId == request.ChildId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new GetAllNotesResponse
                {
                    Id = n.Id,
                    Title = n.NoteTitle,
                    Description = n.NoteContent,
                    CreatedAt = n.CreatedAt
                })
                .AsQueryable();

            var paginated = await PaginatedList<GetAllNotesResponse>.CreateAsync(query, request.PageNumber ?? 1, request.PageSize ?? 10);

            return Result.Success(paginated);
        }
    }
}
