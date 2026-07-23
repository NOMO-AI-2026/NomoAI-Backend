using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Child;

namespace NomoAI.API.Features.Children.Add_Child
{
    public class AddChildCommandHandelr : IRequestHandler<AddChildCommand, Result<AddChildResponseDto>>
    {
        private readonly AppDbContext _db;
        public AddChildCommandHandelr(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<AddChildResponseDto>> Handle(AddChildCommand request, CancellationToken cancellationToken)
        {
            // ensure speech level exists
            var speechLevel = await _db.SpeechLevels.FindAsync(new object[] { request.SpeechLevelId }, cancellationToken);
            if (speechLevel is null)
            {
                return Result.Failure<AddChildResponseDto>(ChildrenErrors.SpeechLevelNotFound);
            }
            if(request.UserId is null)
            {
                return Result.Failure<AddChildResponseDto>(ChildrenErrors.DoctorNotFound);
            }
            int doctorId = _db.Doctor.FirstOrDefault(x => x.UserId == request.UserId &&!x.IsDeleted)?.Id ?? 0;
            if (doctorId == 0) { 
                return Result.Failure<AddChildResponseDto>(ChildrenErrors.DoctorNotFound);
            }
            var child = new Domain.Entities.Children
            {
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                TherapyStartDate = request.TherapyStartDate,
                Age = request.Age,
                SpeechLevelId = request.SpeechLevelId,
                DoctorId = doctorId
            };

            _db.Children.Add(child);
            await _db.SaveChangesAsync(cancellationToken);

            var dto = new AddChildResponseDto
            {
                Id = child.Id,
                FullName = child.FullName,
                Age = child.Age,
                SpeechLevelId = child.SpeechLevelId
            };

            return Result.Success(dto);
        }
    }
}
