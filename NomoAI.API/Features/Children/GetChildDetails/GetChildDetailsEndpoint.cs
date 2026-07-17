using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.GetChildDetails
{
    public class GetChildDetailsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/children/{childId:int}", async (int childId, IMediator mediator) =>
            {
                var query = new GetChildDeatilsQuery(childId);
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("GetChildDetails")
            .WithTags("Children")
            .WithSummary("Get details for a specific child");
        }
    }
}
