using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.ParentDashboard.GetParentDashboard;

public sealed class GetParentDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/parent/dashboard", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Parent"))
            .WithName("GetParentDashboard")
            .WithTags("ParentDashboard")
            .WithSummary("Get parent dashboard child cards")
            .WithDescription(
                "Returns per-child progress, activity stats, and latest doctor note for the authenticated parent.")
            .Produces<GetParentDashboardResponse>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden)
            .Produces<Error>(
                StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<GetParentDashboardResponse> result =
            await sender.Send(
                new GetParentDashboardQuery(userId),
                cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
