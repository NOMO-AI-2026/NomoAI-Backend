using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.SpeechLevels.GetAllLevels
{
    public class GetAllLevelsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/speech-levels", async (IMediator mediator) =>
            {
                var query = new GetAllLevelsQuery();
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .AllowAnonymous()
            .WithName("GetAllSpeechLevels")
            .WithTags("SpeechLevels")
            .WithSummary("Get all speech levels");
        }
    }
}
