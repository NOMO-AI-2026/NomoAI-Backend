namespace NomoAI.API.Features.Children.GetChildSpeechLevelHistory;

public sealed record ChildSpeechLevelHistoryItemResponse(
	int Id,
	int ChildId,
	int PreviousSpeechLevelId,
	string PreviousSpeechLevelName,
	int NewSpeechLevelId,
	string NewSpeechLevelName,
	DateTime ChangedAt,
	string? ChangeReasons);

public sealed record GetChildSpeechLevelHistoryResponse(
	IReadOnlyCollection<ChildSpeechLevelHistoryItemResponse> Items,
	int PageNumber,
	int PageSize,
	int TotalCount,
	int TotalPages,
	bool HasPreviousPage,
	bool HasNextPage);