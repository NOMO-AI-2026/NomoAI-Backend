using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetChildSpeechLevelHistory;

public static class GetChildSpeechLevelHistoryErrors
{
	public static readonly Error ChildNotFound = new(
		"Children.NotFound",
		"The specified child was not found.",
		StatusCodes.Status404NotFound);
}