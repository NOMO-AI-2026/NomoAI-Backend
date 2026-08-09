using FluentValidation;

namespace NomoAI.API.Features.Sessions.Runtime.GetSessionAttempts;

public sealed class GetSessionAttemptsValidator : AbstractValidator<GetSessionAttemptsQuery>
{
    public GetSessionAttemptsValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}
