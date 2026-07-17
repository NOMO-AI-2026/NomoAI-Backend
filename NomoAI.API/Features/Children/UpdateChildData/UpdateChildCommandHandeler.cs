using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;
using NomoAI.API.Domain.Entities;
using AutoMapper;

namespace NomoAI.API.Features.Children.UpdateChildData
{
    internal sealed class UpdateChildCommandHandeler : IRequestHandler<UpdateChildCommand, Result<bool>>
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        public UpdateChildCommandHandeler(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<Result<bool>> Handle(UpdateChildCommand request, CancellationToken cancellationToken)
        {
            var child = await _db.Children
                .Where(c => c.Id == request.ChildId && !c.IsDeleted)
                .SingleOrDefaultAsync(cancellationToken);

            if (child is null)
            {
                return Result.Failure<bool>(new Error("Children.ChildNotFound", "Child not found.", 404));
            }

            // if speech level changed, insert history
            if (child.SpeechLevelId != request.Request.SpeechLevelId)
            {
                var history = new ChildSpeechLevelHistory
                {
                    ChildId = child.Id,
                    PreviousSpeechLevelId = child.SpeechLevelId,
                    NewSpeechLevelId = request.Request.SpeechLevelId,
                    ChangedAt = DateTime.UtcNow,
                    ChangeReasons = request.Request.SpeechLevelChangeReasons
                };

                _db.ChildSpeechLevelHistories.Add(history);
                child.SpeechLevelId = request.Request.SpeechLevelId;
            }
            _mapper.Map(request.Request, child);
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
