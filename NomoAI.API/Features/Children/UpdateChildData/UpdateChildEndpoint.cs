using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.UpdateChildData
{
    public class UpdateChildEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/children/{childId:int}", async (int childId, UpdateChildRequest request, IMediator mediator) =>
            {
                var command = new UpdateChildCommand(childId)
                {
                    Request = request
                };
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok() : result.ToProblem();
            })
            .RequireAuthorization()
            .WithTags("Children")
            .WithName("UpdateChild")
            .WithSummary("Update child data and create history when speech level changes.");
        }
    }
}
