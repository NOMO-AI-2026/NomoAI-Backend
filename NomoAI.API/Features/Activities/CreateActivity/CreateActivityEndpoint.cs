using MediatR;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Activities.CreateActivity;

public static class CreateActivityEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPost("/", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor"))
            .WithName("CreateActivity")
            .WithSummary("Create a new activity")
            .WithDescription(
                "Creates a new activity for a child. " +
                "Only the doctor responsible for the child can create it.")
            .Accepts<CreateActivityRequest>("application/json")
            .Produces<CreateActivityResponse>(
                StatusCodes.Status201Created)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces<Error>(
                StatusCodes.Status403Forbidden)
            .Produces<Error>(
                StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        CreateActivityRequest request,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? doctorUserId = currentUser.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(doctorUserId))
        {
            return Results.Unauthorized();
        }

        var command = new CreateActivityCommand(
            request.ChildId,
            request.ActivityTarget,
            request.Content,
            request.EstimatedDurationMinutes,
            doctorUserId);

        Result<CreateActivityResponse> result =
            await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(
                result.Error,
                statusCode: result.Error.StatusCode);
        }

        return Results.Created(
            $"/api/activities/{result.Value.ActivityId}",
            result.Value);
    }
}