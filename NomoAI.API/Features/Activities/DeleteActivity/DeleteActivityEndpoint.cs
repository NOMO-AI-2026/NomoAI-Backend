using MediatR;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Activities.DeleteActivity;

public static class DeleteActivityEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapDelete("/{activityId:int}", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor"))
            
            .WithName("DeleteActivity")
            .WithSummary("Delete a specific activity")
            .WithDescription(
                "Soft deletes an activity. " +
                "Only the doctor responsible for the child can delete it.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound)
            .Produces<Error>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        int activityId,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? doctorUserId = currentUser
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(doctorUserId))
        {
            return Results.Unauthorized();
        }

        var command = new DeleteActivityCommand(
            activityId,
            doctorUserId);

        Result result = await sender.Send(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(
                result.Error,
                statusCode: result.Error.StatusCode);
        }

        return Results.NoContent();
    }
}