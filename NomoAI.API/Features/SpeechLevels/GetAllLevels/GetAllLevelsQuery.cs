using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.SpeechLevels.GetAllLevels
{
    public record GetAllLevelsQuery() : IRequest<Result<IEnumerable<SpeechLevelResponse>>>;
}
