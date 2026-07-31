using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Analytics.GetSystemAnalyticsOverview;

public class GetSystemAnalyticsOverviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/analytics/overview", HandleAsync)
            .RequireAuthorization(
                policy => policy.RequireRole("Admin"))
            .WithName("GetSystemAnalyticsOverview")
            .WithTags("AdminDashboard")
            .WithSummary(
                "Get system-wide analytics overview for admin dashboard")
            .WithDescription(
                "Returns aggregated platform counters for users, children, therapy, alerts, support, and speech levels.")
            .Produces<GetSystemAnalyticsOverviewResponse>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        Result<GetSystemAnalyticsOverviewResponse> result =
            await sender.Send(
                new GetSystemAnalyticsOverviewQuery(),
                cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
