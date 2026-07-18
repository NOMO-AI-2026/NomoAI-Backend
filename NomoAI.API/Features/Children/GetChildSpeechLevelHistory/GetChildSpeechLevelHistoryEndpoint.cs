using MediatR;
using Microsoft.AspNetCore.Http;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetChildSpeechLevelHistory;

public sealed class GetChildSpeechLevelHistoryEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet(
				"/api/children/{childId:int}/speech-level-history",
				HandleAsync)
			.RequireAuthorization()
			.WithName("GetChildSpeechLevelHistory")
			.WithTags("Children")
			.WithSummary("Get child speech level history")
			.WithDescription(
				"Returns the speech level change history for a specific child with optional filters and pagination.")
			.Produces<GetChildSpeechLevelHistoryResponse>(
				StatusCodes.Status200OK)
			.Produces<Error>(
				StatusCodes.Status400BadRequest)
			.Produces<Error>(
				StatusCodes.Status404NotFound)
			.Produces(
				StatusCodes.Status401Unauthorized);
	}

	private static async Task<IResult> HandleAsync(
		int childId,
		[AsParameters] GetChildSpeechLevelHistoryRequest request,
		ISender sender,
		CancellationToken cancellationToken)
	{
		var query = new GetChildSpeechLevelHistoryQuery(
			ChildId: childId,
			PageNumber: request.PageNumber,
			PageSize: request.PageSize,
			PreviousSpeechLevelId:
				request.PreviousSpeechLevelId,
			NewSpeechLevelId:
				request.NewSpeechLevelId
			//FromDate: request.FromDate,
			//ToDate: request.ToDate
			);

		var result = await sender.Send(
			query,
			cancellationToken);

		//if (result.IsFailure)
		//{ 
		//		return result.ToProblem();
		//}

		////return Results.Ok(result.Value);
		//return Results.Ok(result);
		return result.IsSuccess ? Results.Ok(result) : result.ToProblem();

	}
}