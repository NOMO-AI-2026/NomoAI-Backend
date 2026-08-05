using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorNotes.UpdateNote
{
    public record UpdateNoteCommand(int NoteId, string DoctorUserId, string Title, string Description) : IRequest<Result>;
}
