namespace NomoAI.API.Features.Children.GetChildSpeechLevelHistory;

public sealed class GetChildSpeechLevelHistoryRequest
{
	public int PageNumber { get; init; } = 1;

	public int PageSize { get; init; } = 10;

	public int? PreviousSpeechLevelId { get; init; }

	public int? NewSpeechLevelId { get; init; }

	//public DateOnly? FromDate { get; init; }

	//public DateOnly? ToDate { get; init; }
}