using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Activities.DeleteActivity;

internal sealed class DeleteActivityHandler
    : IRequestHandler<DeleteActivityCommand, Result>
{
    private readonly AppDbContext _dbContext;

    public DeleteActivityHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(
        DeleteActivityCommand request,
        CancellationToken cancellationToken)
    {
        var activityInfo = await _dbContext.Activities
            .AsNoTracking()
            .Where(activity =>
                activity.Id == request.ActivityId &&
                !activity.IsDeleted &&
                !activity.Child.IsDeleted)
            .Select(activity => new
            {
                activity.Id,

                DoctorUserId =
                    activity.Child.Doctor.UserId,

                DoctorIsApproved =
                    activity.Child.Doctor.IsApproved,

                DoctorIsDeleted =
                    activity.Child.Doctor.IsDeleted,

                DoctorUserIsDeleted =
                    activity.Child.Doctor.User.IsDeleted
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (activityInfo is null)
        {
            return Result.Failure(
                DeleteActivityErrors.ActivityNotFound);
        }

        bool belongsToCurrentDoctor =
            activityInfo.DoctorUserId == request.DoctorUserId;

        if (!belongsToCurrentDoctor)
        {
            return Result.Failure(
                DeleteActivityErrors.UnauthorizedActivityAccess);
        }

        if (!activityInfo.DoctorIsApproved ||
            activityInfo.DoctorIsDeleted ||
            activityInfo.DoctorUserIsDeleted)
        {
            return Result.Failure(
                DeleteActivityErrors.DoctorAccountNotAvailable);
        }

        int affectedRows = await _dbContext.Activities
            .Where(activity =>
                activity.Id == request.ActivityId &&
                !activity.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    activity => activity.IsDeleted,
                    true),
                cancellationToken);

        if (affectedRows == 0)
        {
            return Result.Failure(
                DeleteActivityErrors.DeleteFailed);
        }

        return Result.Success();
    }
}