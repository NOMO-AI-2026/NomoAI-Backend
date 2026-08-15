using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails
{
    public class GetDoctorDetailsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/doctors/{userId}", async Task<IResult> (
                string userId,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetDoctorDetailsQuery(userId),
                    cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result)
                    : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("GetDoctorDetails")
            .WithTags("AdminDashboard")
            .WithSummary("Get full doctor details by user id")
            .Produces<Result<DoctorDetailsResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
