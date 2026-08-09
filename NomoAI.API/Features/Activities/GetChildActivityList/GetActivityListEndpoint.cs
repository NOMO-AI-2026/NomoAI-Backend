using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Activities.GetChildActivityList
{
    public class GetActivityListEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/children/{childId:int}/activities", async (
                int childId,
                IMediator mediator,
                bool onlyAvailableForSession = false) =>
            {
                var query = new GetChildActivityListQuery(childId, onlyAvailableForSession);
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
              .RequireAuthorization()
             .WithSummary("Gets activities for a child. Pass onlyAvailableForSession=true to list activities that can still start a therapy session.")
            .WithName("GetChildActivities")
            .WithTags("Activities");
        }
    }
}
