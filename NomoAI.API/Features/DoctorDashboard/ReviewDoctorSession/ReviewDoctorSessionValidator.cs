using FluentValidation;

namespace NomoAI.API.Features.DoctorDashboard.ReviewDoctorSession;

public sealed class ReviewDoctorSessionValidator
    : AbstractValidator<ReviewDoctorSessionCommand>
{
    public ReviewDoctorSessionValidator()
    {
        RuleFor(command => command.SessionId)
            .GreaterThan(0)
            .WithMessage("Session ID must be greater than zero.");

        RuleFor(command => command.DoctorUserId)
            .NotEmpty()
            .WithMessage("Authenticated user ID is required.");

        RuleFor(command => command.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(command => command.Comment)
            .Cascade(CascadeMode.StopOnFirstFailure)
            .NotEmpty()
            .WithMessage("Comment is required.")
            .Must(comment => comment.Trim().Length >= 3)
            .WithMessage("Comment must be at least 3 characters.")
            .Must(comment => comment.Trim().Length <= 1000)
            .WithMessage("Comment cannot exceed 1000 characters.");
    }
}
