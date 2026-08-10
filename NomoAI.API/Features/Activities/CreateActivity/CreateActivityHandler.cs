using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Activities.CreateActivity;

internal sealed class CreateActivityHandler
    : IRequestHandler<
        CreateActivityCommand,
        Result<CreateActivityResponse>>
{
    private readonly AppDbContext _dbContext;

    public CreateActivityHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CreateActivityResponse>> Handle(
        CreateActivityCommand request,
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
            return Result.Failure<CreateActivityResponse>(
                CreateActivityErrors.DoctorProfileNotFound);
        }

        if (!doctor.IsApproved)
        {
            return Result.Failure<CreateActivityResponse>(
                CreateActivityErrors.DoctorNotApproved);
        }
        if (request.CanMakeSession == true)
        {
            int avialableMinutes = await _dbContext.DoctorCreditWallets.Where(dc => dc.DoctorId == doctor.Id)
                   .Select(dc => dc.AvailableMinutes)
                   .FirstOrDefaultAsync(cancellationToken);

            int estimatedMinutes = await _dbContext.Activities
                .Where(a => !a.IsDeleted && a.CanMakeSession && a.Child.DoctorId == doctor.Id)
                .Select(s => s.EstimatedDurationMinutes)
                .SumAsync(cancellationToken);

            estimatedMinutes += request.EstimatedDurationMinutes;

            if (estimatedMinutes > avialableMinutes)
            {
                return Result.Failure<CreateActivityResponse>(CreateActivityErrors.InsufficientCredit);
            }
        }

        bool childExists = await _dbContext.Children
            .AsNoTracking()
            .AnyAsync(
                child =>
                    child.Id == request.ChildId &&
                    !child.IsDeleted,
                cancellationToken);

        if (!childExists)
        {
            return Result.Failure<CreateActivityResponse>(
                CreateActivityErrors.ChildNotFound);
        }

        bool childBelongsToDoctor = await _dbContext.Children
            .AsNoTracking()
            .AnyAsync(
                child =>
                    child.Id == request.ChildId &&
                    child.DoctorId == doctor.Id &&
                    !child.IsDeleted,
                cancellationToken);

        if (!childBelongsToDoctor)
        {
            return Result.Failure<CreateActivityResponse>(
                CreateActivityErrors.ChildDoesNotBelongToDoctor);
        }

        var activity =
            new NomoAI.API.Domain.Entities.Activity
            {
                ChildId = request.ChildId,
                ActivityTarget = request.ActivityTarget,
                Content = request.Content.Trim(),
                EstimatedDurationMinutes =
                    request.EstimatedDurationMinutes,
                CanMakeSession = request.CanMakeSession
            };

        _dbContext.Activities.Add(activity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreateActivityResponse(
            activity.Id,
            activity.ChildId,
            activity.ActivityTarget,
            activity.Content,
            activity.EstimatedDurationMinutes,
            activity.CreatedAt,
            "Activity created successfully.");

        return Result.Success(response);
    }
}