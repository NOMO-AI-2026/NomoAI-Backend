using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.AdminDashboard.GetAllDoctors
{
    public class GerAllApprovedDoctorEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/doctors", async Task<IResult> (bool isApproved, int pageNumber, int pageSize, IMediator mediator) =>
            {
                var query = new GetAllDoctorsQuery(isApproved, pageNumber <=0 ?1 : pageNumber, pageSize <=0 ?10 : pageSize);
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("GetAllDoctors")
            .WithTags("AdminDashboard")
            .WithSummary("Get doctors list with pagination and approval filter")
            .Produces<PaginatedList<DoctorResponse>>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized);
        }
    }
}
