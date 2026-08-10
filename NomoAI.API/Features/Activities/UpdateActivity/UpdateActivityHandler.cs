using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Activities.CreateActivity;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Activities.UpdateActivity;

internal sealed class UpdateActivityHandler
    : IRequestHandler<
        UpdateActivityCommand,
        Result<UpdateActivityResponse>>
{
    private readonly AppDbContext _dbContext;

    public UpdateActivityHandler(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<UpdateActivityResponse>> Handle(
        UpdateActivityCommand request,
        CancellationToken cancellationToken)
    {
        var doctor = await _dbContext
            .Set<Doctor>()
            .AsNoTracking()
            .Where(doctor =>
                doctor.UserId == request.DoctorUserId &&
                !doctor.IsDeleted)
            .Select(doctor => new
            {
                doctor.Id,
                doctor.IsApproved
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (doctor is null)
        {
            return Result.Failure<UpdateActivityResponse>(
                UpdateActivityErrors.DoctorProfileNotFound);
        }

        if (!doctor.IsApproved)
        {
            return Result.Failure<UpdateActivityResponse>(
                UpdateActivityErrors.DoctorNotApproved);
        }


        var activity = await _dbContext.Activities
            .AsNoTracking()
            .Where(activity =>
                activity.Id == request.ActivityId &&
                !activity.IsDeleted)
            .Select(activity => new
            {
                activity.Id,
                activity.ChildId,
                activity.CanMakeSession,
                activity.EstimatedDurationMinutes
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (activity is null)
        {
            return Result.Failure<UpdateActivityResponse>(
                UpdateActivityErrors.ActivityNotFound);
        }

        if (request.CanMakeSession == true && activity.CanMakeSession == false)
        {
            int avialableMinutes = getDoctorAvailableMinutes(doctor.Id);

            int estimatedMinutes = getDoctorEstimatedMinutes(doctor.Id);

            estimatedMinutes += request.EstimatedDurationMinutes;

            if (estimatedMinutes > avialableMinutes)
            {
                return Result.Failure<UpdateActivityResponse>(UpdateActivityErrors.InsufficientCredit);
            }
        }
        if(request.CanMakeSession == true && activity.EstimatedDurationMinutes < request.EstimatedDurationMinutes)
        {
            int avialableMinutes = getDoctorAvailableMinutes(doctor.Id);

            int estimatedMinutes = getDoctorEstimatedMinutes(doctor.Id);

            estimatedMinutes += (request.EstimatedDurationMinutes - activity.EstimatedDurationMinutes);

            if (estimatedMinutes > avialableMinutes)
            {
                return Result.Failure<UpdateActivityResponse>(UpdateActivityErrors.InsufficientCredit);
            }
        }
        bool childBelongsToDoctor =
            await _dbContext.Children
                .AsNoTracking()
                .AnyAsync(
                    child =>
                        child.Id == activity.ChildId &&
                        child.DoctorId == doctor.Id &&
                        !child.IsDeleted,
                    cancellationToken);

        if (!childBelongsToDoctor)
        {
            return Result.Failure<UpdateActivityResponse>(
                UpdateActivityErrors
                    .UnauthorizedActivityAccess);
        }

        string normalizedContent =
            request.Content.Trim();

        int affectedRows = await _dbContext.Activities
            .Where(activity =>
                activity.Id == request.ActivityId &&
                !activity.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        activity => activity.ActivityTarget,
                        request.ActivityTarget)
                    .SetProperty(
                        activity => activity.Content,
                        normalizedContent)
                    .SetProperty(
                        activity =>
                            activity.EstimatedDurationMinutes,
                        request.EstimatedDurationMinutes)
                    .SetProperty(
                        activity => activity.CanMakeSession,
                        request.CanMakeSession),
                cancellationToken);

        if (affectedRows == 0)
        {
            return Result.Failure<UpdateActivityResponse>(
                UpdateActivityErrors.UpdateFailed);
        }

        var response = new UpdateActivityResponse(
            activity.Id,
            activity.ChildId,
            request.ActivityTarget,
            normalizedContent,
            request.EstimatedDurationMinutes,
            request.CanMakeSession,
            "Activity updated successfully.");

        return Result.Success(response);
    }
    private int getDoctorAvailableMinutes(int doctorId)
    {
        int avialableMinutes = _dbContext.DoctorCreditWallets.Where(dc => dc.DoctorId == doctorId)
                   .Select(dc => dc.AvailableMinutes)
                   .FirstOrDefault();
        return avialableMinutes;
    }
    private int getDoctorEstimatedMinutes(int doctorId)
    {
        int estimatedMinutes = _dbContext.Activities
            .Where(a => !a.IsDeleted && a.CanMakeSession && a.Child.DoctorId == doctorId)
            .Select(s => s.EstimatedDurationMinutes)
            .Sum();
        return estimatedMinutes;
    }
}