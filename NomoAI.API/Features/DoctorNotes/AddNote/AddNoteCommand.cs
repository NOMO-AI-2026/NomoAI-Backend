using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorNotes.AddNote
{
    public record AddNoteCommand(int ChildId, string DoctorUserId, string NoteTitle, string NoteContent) : IRequest<Result<int>>;
}
