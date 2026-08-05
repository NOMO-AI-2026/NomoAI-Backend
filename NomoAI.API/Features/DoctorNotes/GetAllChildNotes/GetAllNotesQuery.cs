using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorNotes.GetAllChildNotes
{
    public record GetAllNotesQuery(int ChildId, int? PageNumber, int? PageSize) : IRequest<Result<PaginatedList<GetAllNotesResponse>>>;
}
