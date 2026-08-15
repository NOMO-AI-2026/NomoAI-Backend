using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails;

public sealed class GetDoctorDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/doctors/{userId}", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("GetDoctorDetails")
            .WithTags("AdminDashboard")
            .WithSummary("Get doctor details including verification documents")
            .WithDescription(
                "Returns identity, profile, approval status, and verification document URLs " +
                "for a non-deleted doctor so an admin can review before approval.")
            .Produces<Result<DoctorDetailsResponse>>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        string userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetDoctorDetailsQuery(userId);
        Result<DoctorDetailsResponse> result =
            await mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblem();
    }
}
