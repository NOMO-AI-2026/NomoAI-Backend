using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Plans.AddPlan
{
    public class AddPlanEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/plans", async Task<IResult> (
                AddPlanRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new AddPlanCommand(
                    request.Name,
                    request.Description,
                    request.IncludedMinutes,
                    request.Price,
                    request.Currency);

                var result = await mediator.Send(command, cancellationToken);

                return result.IsSuccess
                    ? Results.Created($"/api/plans/{result.Value}", result)
                    : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("AddPlan")
            .WithTags("Plans")
            .WithSummary("Add a new subscription plan")
            .WithDescription("Currency = 1 for EGP and 0 for USD , Please  send always one and show the admin the money in USD")
            .Accepts<AddPlanRequest>("application/json")
            .Produces<Result<int>>(StatusCodes.Status201Created)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status409Conflict);
        }
    }
}
