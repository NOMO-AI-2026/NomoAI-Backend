using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.DoctorNotes.UpdateNote
{
    internal sealed class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand, Result>
    {
        private readonly AppDbContext _db;
        public UpdateNoteCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
        {
            var doctor = await _db.Doctor
                .AsNoTracking()
                .Where(d => d.UserId == request.DoctorUserId && !d.IsDeleted)
                .Select(d => new { d.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (doctor is null)
            {
                return Result.Failure(DoctorNotesErrors.DoctorNotFound);
            }


            var note = await _db.DoctorNotes
                .Where(n => n.Id == request.NoteId && !n.IsDeleted)
                .Select(n => new { n.Id, n.ChildId, n.DoctorId })
                .SingleOrDefaultAsync(cancellationToken);

            if (note is null)
            {
                return Result.Failure(DoctorNotesErrors.NoteNotFound);
            }

            // ensure the doctor owns the note
            if (note.DoctorId != doctor.Id)
            {
                return Result.Failure(DoctorNotesErrors.UnathorizedAccess);
            }

            int affected = await _db.DoctorNotes
                .Where(n => n.Id == request.NoteId && !n.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.NoteTitle, request.Title.Trim())
                    .SetProperty(n => n.NoteContent, request.Description.Trim()),
                    cancellationToken);

            if (affected == 0)
            {
                return Result.Failure(DoctorNotesErrors.UpdateFailed);
            }

            return Result.Success();
        }
    }
}
