using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Sessions.Runtime.GetSessionAttempts;

public sealed class GetSessionAttemptsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sessions/{sessionId:int}/attempts", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor"))
            .WithName("GetSessionAttempts")
            .WithSummary("Get all attempts for a therapy session, including evaluation and transcription. Doctor only.")
            .WithTags("Sessions Runtime")
            .Produces<GetSessionAttemptsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        int sessionId,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<GetSessionAttemptsResponse> result =
            await sender.Send(new GetSessionAttemptsQuery(sessionId, userId), cancellationToken);

        if (result.IsFailure)
        {
            return Results.Json(result.Error, statusCode: result.Error.StatusCode);
        }

        return Results.Ok(result.Value);
    }
}
