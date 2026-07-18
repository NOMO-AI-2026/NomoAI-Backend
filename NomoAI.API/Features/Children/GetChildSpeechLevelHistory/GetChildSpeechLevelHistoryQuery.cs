using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetChildSpeechLevelHistory;

public sealed record GetChildSpeechLevelHistoryQuery(
	int ChildId,
	int PageNumber,int PageSize,int? PreviousSpeechLevelId,int? NewSpeechLevelId): IRequest<Result<PaginatedList<ChildSpeechLevelHistoryItemResponse>>>;