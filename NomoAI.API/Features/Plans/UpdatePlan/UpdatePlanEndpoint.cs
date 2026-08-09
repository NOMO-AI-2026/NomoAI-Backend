using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Plans.UpdatePlan
{
    public class UpdatePlanEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/plans/{planId:int}", async Task<IResult> (
                int planId,
                UpdatePlanRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdatePlanCommand(
                    planId,
                    request.Name,
                    request.Description,
                    request.IncludedMinutes,
                    request.Price,
                    request.Currency);

                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("UpdatePlan")
            .WithTags("Plans")
            .WithSummary("Update a subscription plan")
            .Accepts<UpdatePlanRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound)
            .Produces<Error>(StatusCodes.Status409Conflict);
        }
    }
}
