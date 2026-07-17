using FluentValidation;

namespace NomoAI.API.Features.Children.Add_Child
{
    public class AddChildCommandValidator: AbstractValidator<AddChildCommand>
    {
        public AddChildCommandValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().WithMessage("FullName is required.");
            RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be greater than 0.");
            RuleFor(x => x.SpeechLevelId).GreaterThan(0).WithMessage("SpeechLevelId is required.");
        }
    }
}
