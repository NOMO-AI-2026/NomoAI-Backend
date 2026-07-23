using MediatR;
using Microsoft.AspNetCore.Mvc;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.DeleteParent
{
    public class DeleteParentEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/admin/parents", async Task<IResult> ([FromBody]DeleteParentRequest request, IMediator mediator) =>
            {
                var command = new DeleteParentCommand(request.UserId);
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("DeleteParent")
            .WithTags("AdminDashboard")
            .WithSummary("Delete parent account")
            .Accepts<DeleteParentRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
