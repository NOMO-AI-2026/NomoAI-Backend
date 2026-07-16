using MediatR;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Children.AssignChildToParent;

public static class AssignChildToParentEndpoint
{
    public static void MapEndpoint(RouteGroupBuilder group)
    {
        group
            .MapPut("/{childId:int}/parent", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor"))
              .AllowAnonymous()
            .WithName("AssignChildToParent")
            .WithSummary("Assign a child to a parent")
            .WithDescription(
                "Assigns or reassigns a child to a parent. " +
                "Only the doctor responsible for the child can perform this operation.")
            .Accepts<AssignChildToParentRequest>("application/json")
            .Produces<AssignChildToParentResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces<Error>(
                StatusCodes.Status403Forbidden)
            .Produces<Error>(
                StatusCodes.Status404NotFound)
            .Produces(
                StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleAsync(
     int childId,
     AssignChildToParentRequest request,
     ClaimsPrincipal currentUser,
     ISender sender,
     CancellationToken cancellationToken)
    {
        string? doctorUserId = currentUser
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (string.IsNullOrWhiteSpace(doctorUserId))
        {
            return Results.Unauthorized();
        }

        var command = new AssignChildToParentCommand(
            childId,
            request.ParentId,
            doctorUserId);

        Result<AssignChildToParentResponse> result =
            await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(
                result.Error,
                statusCode: result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}