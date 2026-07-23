using MediatR;
using Microsoft.AspNetCore.Mvc;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.DeleteDoctor
{
    public class DeleteDoctorEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/admin/doctors", async Task<IResult> ([FromBody]DeleteDoctorRequest request, IMediator mediator) =>
            {
                var command = new DeleteDoctorCommand(request.UserId);
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("DeleteDoctor")
            .WithTags("AdminDashboard")
            .WithSummary("Delete doctor account")
            .Accepts<DeleteDoctorRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
