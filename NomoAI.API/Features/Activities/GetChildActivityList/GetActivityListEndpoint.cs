using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Activities.GetChildActivityList
{
    public class GetActivityListEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/children/{childId:int}/activities", async (int childId, IMediator mediator) =>
            {
                var query = new GetChildActivityListQuery(childId);
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
              .RequireAuthorization()
             .WithSummary("gets the current activities of a child")
            .WithName("GetChildActivities")
            .WithTags("Activities");
        }
    }
}
