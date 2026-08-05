using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorNotes.DeleteNote
{
    public record DeleteNoteQuery(int NoteId, string DoctorUserId) : IRequest<Result>;
}
