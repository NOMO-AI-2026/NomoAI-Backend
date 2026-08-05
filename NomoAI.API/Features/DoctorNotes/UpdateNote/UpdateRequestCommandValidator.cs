using FluentValidation;

namespace NomoAI.API.Features.DoctorNotes.UpdateNote
{
    public class UpdateRequestCommandValidator : AbstractValidator<UpdateNoteRequest>
    {
        public UpdateRequestCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
        }
    }
}
