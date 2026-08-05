using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;
using NomoAI.API.Domain.Entities;

namespace NomoAI.API.Features.DoctorNotes.AddNote
{
    internal sealed class AddNoteCommandHandler : IRequestHandler<AddNoteCommand, Result<int>>
    {
        private readonly AppDbContext _db;
        public AddNoteCommandHandler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<int>> Handle(AddNoteCommand request, CancellationToken cancellationToken)
        {
            // validate child exists
            var childExists = await _db.Children
                .AsNoTracking()
                .AnyAsync(c => c.Id == request.ChildId && !c.IsDeleted, cancellationToken);

            if (!childExists)
            {
                return Result.Failure<int>(DoctorNotesErrors.ChildNotFound);
            }

            // find doctor by user id
            var doctor = await _db.Doctor
                .Where(d => d.UserId == request.DoctorUserId && !d.IsDeleted && d.IsApproved)
                .SingleOrDefaultAsync(cancellationToken);

            if (doctor is null)
            {
                return Result.Failure<int>(DoctorNotesErrors.DoctorNotFound);
            }

            var note = new NomoDoc.Domain.Entities.DoctorNotes
            {
                DoctorId = doctor.Id,
                ChildId = request.ChildId,
                NoteTitle = request.NoteTitle.Trim(),
                NoteContent = request.NoteContent.Trim()
            };

            _db.DoctorNotes.Add(note);
            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success(note.Id);
        }
    }
}
