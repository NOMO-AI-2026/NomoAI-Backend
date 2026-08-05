using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.DoctorNotes.DeleteNote
{
    internal sealed class DeleteNoteQueryHandler : IRequestHandler<DeleteNoteQuery, Result>
    {
        private readonly AppDbContext _db;
        public DeleteNoteQueryHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result> Handle(DeleteNoteQuery request, CancellationToken cancellationToken)
        {
            // verify doctor
            var doctor = await _db.Doctor
                .AsNoTracking()
                .Where(d => d.UserId == request.DoctorUserId && !d.IsDeleted)
                .Select(d => new { d.Id, d.IsApproved })
                .SingleOrDefaultAsync(cancellationToken);

            if (doctor is null)
            {
                return Result.Failure(DoctorNotesErrors.DoctorNotFound);
            }


            var note = await _db.DoctorNotes
                .Where(n => n.Id == request.NoteId && !n.IsDeleted)
                .Select(n => new { n.Id, n.DoctorId })
                .SingleOrDefaultAsync(cancellationToken);

            if (note is null)
            {
                return Result.Failure(DoctorNotesErrors.NoteNotFound);
            }

            if (note.DoctorId != doctor.Id)
            {
                return Result.Failure(DoctorNotesErrors.UnathorizedAccess);
            }

            int affected = await _db.DoctorNotes
                .Where(n => n.Id == request.NoteId && !n.IsDeleted)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsDeleted, true), cancellationToken);

            if (affected ==0)
            {
                return Result.Failure(DoctorNotesErrors.DeleteFailed);
            }

            return Result.Success();
        }
    }
}
