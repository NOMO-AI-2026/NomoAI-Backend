using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Child;
using NomoAI.API.Features.SpeechLevels.GetAllLevels;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Children.GetChildDetails
{
    internal sealed class GetChildDeatailsHandeler : IRequestHandler<GetChildDeatilsQuery, Result<ChildDeatailsResponse>>
    {
        private readonly AppDbContext _db;
        public GetChildDeatailsHandeler(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<ChildDeatailsResponse>> Handle(GetChildDeatilsQuery request, CancellationToken cancellationToken)
        {
            if (request.Role == "Parent")
            {
                int ParentId = await _db.Parents
                    .Where(p => p.UserId == request.UserId && !p.IsDeleted)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (ParentId == 0)
                {
                    return Result.Failure<ChildDeatailsResponse>(ChildrenErrors.ParentNotFound);
                }
                bool isTrueParent = await _db.Children.AnyAsync(c => c.Id == request.ChildId && c.ParentId == ParentId && !c.IsDeleted, cancellationToken);

                if (!isTrueParent)
                {
                    return Result.Failure<ChildDeatailsResponse>(ChildrenErrors.AccessDenied);
                }
            }
            else
            {
                int DoctorId = await _db.Doctor
                    .Where(p => p.UserId == request.UserId && !p.IsDeleted)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                bool isTrueDoctor = await _db.Children.AnyAsync(c => c.Id == request.ChildId && c.DoctorId == DoctorId && !c.IsDeleted, cancellationToken);

                if (!isTrueDoctor)
                {
                    return Result.Failure<ChildDeatailsResponse>(ChildrenErrors.AccessDenied);
                }
            }
        
            var child = await _db.Children
                .AsNoTracking()
                .Where(c => c.Id == request.ChildId && !c.IsDeleted)
                .Select(c => new ChildDeatailsResponse
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    DateOfBirth = c.DateOfBirth,
                    Gender = c.Gender,
                    TherapyStartDate = c.TherapyStartDate,
                    Age = c.Age,
                    ParentFullName = c.Parent != null && c.Parent!.IsDeleted == false? c.Parent.User.Fullname : null,
                    ParentEmail = c.Parent != null && c.Parent!.IsDeleted == false? c.Parent.User.Email ?? string.Empty : null,
                    ParentPhoneNumber = c.Parent != null && c.Parent!.IsDeleted == false? c.Parent.User.PhoneNumber : null,
                    speechLevel = new SpeechLevelResponse
                    {
                        Id = c.SpeechLevel.Id,
                        LevelName = c.SpeechLevel.LevelName
                    } 

                })
                .SingleOrDefaultAsync(cancellationToken);

            if (child is null)
            {
                return Result.Failure<ChildDeatailsResponse>(new Error("Children.ChildNotFound", "Child not found.",404));
            }

            return Result.Success(child);
        }
    }
}
