using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.SpeechLevels.GetAllLevels
{
    internal sealed class GetAllLevelsQueryHandeler : IRequestHandler<GetAllLevelsQuery, Result<IEnumerable<SpeechLevelResponse>>>
    {
        private readonly AppDbContext _db;
        public GetAllLevelsQueryHandeler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<IEnumerable<SpeechLevelResponse>>> Handle(GetAllLevelsQuery request, CancellationToken cancellationToken)
        {
            var levels = await _db.SpeechLevels
                .AsNoTracking()
                .Where(l => !l.IsDeleted)
                .Select(l => new SpeechLevelResponse
                {
                    Id = l.Id,
                    LevelName = l.LevelName
                })
                .ToListAsync(cancellationToken);

            return Result.Success<IEnumerable<SpeechLevelResponse>>(levels);
        }
    }
}
