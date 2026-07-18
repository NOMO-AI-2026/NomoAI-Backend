using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
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
                    ParentFullName = c.Parent != null ? c.Parent.User.Fullname : null,
                    ParentEmail = c.Parent != null ? c.Parent.User.Email ?? string.Empty : null,
                    ParentPhoneNumber = c.Parent != null ? c.Parent.User.PhoneNumber : null
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
