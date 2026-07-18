using Azure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Features.Auth.Login_User;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Children.GetChildSpeechLevelHistory;

public sealed class GetChildSpeechLevelHistoryHandler
	: IRequestHandler<GetChildSpeechLevelHistoryQuery, Result<PaginatedList<ChildSpeechLevelHistoryItemResponse>>>
{
	private readonly AppDbContext _dbContext;

	public GetChildSpeechLevelHistoryHandler(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<Result<PaginatedList<ChildSpeechLevelHistoryItemResponse>>> Handle(
	GetChildSpeechLevelHistoryQuery request,
	CancellationToken cancellationToken)
	{
		var childExists = await _dbContext.Children
			.AsNoTracking()
			.AnyAsync(
				child => child.Id == request.ChildId,
				cancellationToken);

		if (!childExists)
		{
			return Result.Failure<
				PaginatedList<ChildSpeechLevelHistoryItemResponse>>(GetChildSpeechLevelHistoryErrors.ChildNotFound);
		}

		var historyQuery = _dbContext.ChildSpeechLevelHistories
			.AsNoTracking()
			.Where(history => history.ChildId == request.ChildId);

		if (request.PreviousSpeechLevelId.HasValue)
		{
			historyQuery = historyQuery.Where(history =>
				history.PreviousSpeechLevelId ==
				request.PreviousSpeechLevelId.Value);
		}

		if (request.NewSpeechLevelId.HasValue)
		{
			historyQuery = historyQuery.Where(history =>
				history.NewSpeechLevelId ==
				request.NewSpeechLevelId.Value);
		}

		//if (request.FromDate.HasValue)
		//{
		//	var fromDate = request.FromDate.Value
		//		.ToDateTime(TimeOnly.MinValue);

		//	historyQuery = historyQuery.Where(history =>
		//		history.ChangedAt >= fromDate);
		//}

		//if (request.ToDate.HasValue)
		//{
		//	var toDateExclusive = request.ToDate.Value
		//		.AddDays(1)
		//		.ToDateTime(TimeOnly.MinValue);

		//	historyQuery = historyQuery.Where(history =>
		//		history.ChangedAt < toDateExclusive);
		//}

		var projectedQuery = historyQuery
			.OrderByDescending(history => history.ChangedAt)
			.ThenByDescending(history => history.Id)
			.Select(history =>
				new ChildSpeechLevelHistoryItemResponse(
					history.Id,
					history.ChildId,
					history.PreviousSpeechLevelId,
					history.PreviousSpeechLevel.LevelName,
					history.NewSpeechLevelId,
					history.NewSpeechLevel.LevelName,
					history.ChangedAt,
					history.ChangeReasons));
		//var response = new ChildHistoryLevelDto
		//{
		 
		//};
		var paginatedHistory =
			await PaginatedList<ChildSpeechLevelHistoryItemResponse>
				.CreateAsync(projectedQuery,request.PageNumber,request.PageSize);
		//return Result<LoginResponseDto>.Success(response);
		return Result<PaginatedList<ChildSpeechLevelHistoryItemResponse>>.Success(paginatedHistory);
	}
}