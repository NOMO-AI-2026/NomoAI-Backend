using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.DoctorDashboard.GetDoctorDashboard;

public sealed class GetDoctorDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/doctor/dashboard", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor"))
            .WithName("GetDoctorDashboard")
            .WithTags("DoctorDashboard")
            .WithSummary("Get doctor dashboard aggregates")
            .WithDescription(
                "Returns session counters and last-7-days speech-level progress lists for the authenticated doctor.")
            .Produces<GetDoctorDashboardResponse>(
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

        Result<GetDoctorDashboardResponse> result =
            await sender.Send(
                new GetDoctorDashboardQuery(userId),
                cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
