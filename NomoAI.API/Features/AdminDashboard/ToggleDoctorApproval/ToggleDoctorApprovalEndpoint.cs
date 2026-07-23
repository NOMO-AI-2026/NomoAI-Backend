using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.ToggleDoctorApproval
{
    public class ToggleDoctorApprovalEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/api/admin/doctors/approval", async Task<IResult> ( ToggleApprovalRequest request, IMediator mediator) =>
            {
                var command = new ToggleDoctorApprovalCommand(request.UserId, request.ApproveStatus);
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("ToggleDoctorApproval")
            .WithTags("AdminDashboard")
            .WithSummary("Approve or disapprove a doctor")
            .Accepts<ToggleApprovalRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
