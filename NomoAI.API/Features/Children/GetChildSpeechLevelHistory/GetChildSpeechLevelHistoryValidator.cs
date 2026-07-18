using FluentValidation;

namespace NomoAI.API.Features.Children.GetChildSpeechLevelHistory;

public sealed class GetChildSpeechLevelHistoryValidator
	: AbstractValidator<GetChildSpeechLevelHistoryQuery>
{
	public GetChildSpeechLevelHistoryValidator()
	{
		RuleFor(x => x.ChildId)
			.GreaterThan(0)
			.WithMessage("ChildId must be greater than zero.");

		RuleFor(x => x.PageNumber)
			.GreaterThan(0)
			.WithMessage("PageNumber must be greater than zero.");

		RuleFor(x => x.PageSize)
			.InclusiveBetween(1, 100)
			.WithMessage("PageSize must be between 1 and 100.");

		RuleFor(x => x.PreviousSpeechLevelId)
			.GreaterThan(0)
			.When(x => x.PreviousSpeechLevelId.HasValue)
			.WithMessage("PreviousSpeechLevelId must be greater than zero.");

		RuleFor(x => x.NewSpeechLevelId)
			.GreaterThan(0)
			.When(x => x.NewSpeechLevelId.HasValue)
			.WithMessage("NewSpeechLevelId must be greater than zero.");

		//RuleFor(x => x.ToDate)
		//	.Must((query, toDate) =>
		//		!query.FromDate.HasValue ||
		//		!toDate.HasValue ||
		//		toDate.Value >= query.FromDate.Value)
		//	.WithMessage("ToDate must be greater than or equal to FromDate.");
	}
}