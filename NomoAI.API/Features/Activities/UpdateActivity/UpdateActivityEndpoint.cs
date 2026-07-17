using MediatR;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Activities.UpdateActivity;

public static class UpdateActivityEndpoint
{
    public static void MapEndpoint(
        RouteGroupBuilder group)
    {
        group
            .MapPut("/{activityId:int}", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor"))
            .WithName("UpdateActivity")
            .WithSummary("Update a specific activity")
            .WithDescription(
                "Updates the target, content, and estimated " +
                "duration of a specific activity. " +
                "Only the doctor responsible for the child " +
                "can update the activity.")
            .Accepts<UpdateActivityRequest>(
                "application/json")
            .Produces<UpdateActivityResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces<Error>(
                StatusCodes.Status403Forbidden)
            .Produces<Error>(
                StatusCodes.Status404NotFound)
            .Produces<Error>(
                StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        int activityId,
        UpdateActivityRequest request,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? doctorUserId =
            currentUser.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(doctorUserId))
        {
            return Results.Unauthorized();
        }

        var command = new UpdateActivityCommand(
            activityId,
            request.ActivityTarget,
            request.Content,
            request.EstimatedDurationMinutes,
            doctorUserId);

        Result<UpdateActivityResponse> result =
            await sender.Send(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(
                result.Error,
                statusCode:
                    result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}