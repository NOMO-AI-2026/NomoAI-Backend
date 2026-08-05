using FluentValidation;

namespace NomoAI.API.Features.DoctorNotes.AddNote
{
    public class AddNoteCommandValidator : AbstractValidator<AddNoteCommand>
    {
        public AddNoteCommandValidator()
        {
            RuleFor(x => x.ChildId).GreaterThan(0).WithMessage("ChildId is required.");
            RuleFor(x => x.NoteTitle).NotEmpty().WithMessage("NoteTitle is required.");
            RuleFor(x => x.NoteContent).NotEmpty().WithMessage("NoteContent is required.");
        }
    }
}
