using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Children.AssignChildToParent;

internal sealed class AssignChildToParentHandler
    : IRequestHandler<
        AssignChildToParentCommand,
        Result<AssignChildToParentResponse>>
{
    private readonly AppDbContext _dbContext;

    public AssignChildToParentHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AssignChildToParentResponse>> Handle(
        AssignChildToParentCommand request,
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
            return Result.Failure<AssignChildToParentResponse>(
                AssignChildToParentErrors.DoctorProfileNotFound);
        }

        if (!doctor.IsApproved)
        {
            return Result.Failure<AssignChildToParentResponse>(
                AssignChildToParentErrors.DoctorNotApproved);
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
            return Result.Failure<AssignChildToParentResponse>(
                AssignChildToParentErrors.ChildNotFound);
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
            return Result.Failure<AssignChildToParentResponse>(
                AssignChildToParentErrors.ChildDoesNotBelongToDoctor);
        }

        var parent = await _dbContext.Parents
            .AsNoTracking()
            .Where(parent =>
                parent.Id == request.ParentId &&
                !parent.IsDeleted &&
                !parent.User.IsDeleted)
            .Select(parent => new
            {
                parent.Id,
                parent.User.Fullname
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (parent is null)
        {
            return Result.Failure<AssignChildToParentResponse>(
                AssignChildToParentErrors.ParentNotFound);
        }

        await _dbContext.Children
            .Where(child =>
                child.Id == request.ChildId &&
                child.DoctorId == doctor.Id &&
                !child.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    child => child.ParentId,
                    parent.Id),
                cancellationToken);

        var response = new AssignChildToParentResponse(
            request.ChildId,
            parent.Id,
            parent.Fullname,
            "Child assigned to parent successfully.");

        return Result.Success(response);
    }
}