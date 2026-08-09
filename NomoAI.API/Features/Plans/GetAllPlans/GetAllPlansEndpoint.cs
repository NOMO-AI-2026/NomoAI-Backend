using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Plans.GetAllPlans
{
    public class GetAllPlansEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/plans", async Task<IResult> (
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetAllPlansQuery(), cancellationToken);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization()
            .AllowAnonymous()
            .WithName("GetAllPlans")
            .WithTags("Plans")
            .WithSummary("Get all subscription plans")
            .Produces<Result<IEnumerable<PlanResponse>>>(StatusCodes.Status200OK);
        }
    }
}
