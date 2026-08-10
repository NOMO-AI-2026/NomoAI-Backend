using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.DoctorDashboard.GetDoctorDashboard;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.DoctorDashboard.ReviewDoctorSession;

internal sealed class ReviewDoctorSessionHandler
    : IRequestHandler<
        ReviewDoctorSessionCommand,
        Result<ReviewDoctorSessionResponse>>
{
    private readonly AppDbContext _dbContext;

    public ReviewDoctorSessionHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ReviewDoctorSessionResponse>> Handle(
        ReviewDoctorSessionCommand request,
        CancellationToken cancellationToken)
    {
        var doctor = await _dbContext.Doctor
            .AsNoTracking()
            .Where(d =>
                d.UserId == request.DoctorUserId &&
                !d.IsDeleted)
            .Select(d => new
            {
                d.Id,
                d.IsApproved
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (doctor is null)
        {
            return Result.Failure<ReviewDoctorSessionResponse>(
                DoctorDashboardErrors.DoctorNotFound);
        }

        //if (!doctor.IsApproved)
        //{
        //    return Result.Failure<ReviewDoctorSessionResponse>(
        //        DoctorDashboardErrors.DoctorNotApproved);
        //}

        var session = await _dbContext.Sessions
            .Include(s => s.Child)
            .Include(s => s.Activity)
            .Where(s =>
                s.Id == request.SessionId &&
                !s.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null ||
            session.Child.IsDeleted ||
            session.Child.DoctorId != doctor.Id)
        {
            return Result.Failure<ReviewDoctorSessionResponse>(
                DoctorSessionReviewErrors.SessionNotFound);
        }

        if (session.Status != SessionStatus.Completed)
        {
            return Result.Failure<ReviewDoctorSessionResponse>(
                DoctorSessionReviewErrors.SessionNotCompleted);
        }


        string trimmedComment = request.Comment.Trim();
        DateTime reviewedAtUtc = DateTime.UtcNow;

        session.DoctorRating = request.Rating;
        session.DoctorComment = trimmedComment;
        session.IsDoctorReviewed = true;
        if(request.repeatSession  != session.Activity.CanMakeSession && request.repeatSession ==  true)
        {
            int avialableMinutes = await _dbContext.DoctorCreditWallets.Where(dc => dc.DoctorId == doctor.Id)
                .Select(dc => dc.AvailableMinutes)
                .FirstOrDefaultAsync(cancellationToken);

            int estimatedMinutes = await _dbContext.Activities
                .Where(a => !a.IsDeleted && a.CanMakeSession && a.Child.DoctorId == doctor.Id)
                .Select(s => s.EstimatedDurationMinutes)
                .SumAsync(cancellationToken);

            estimatedMinutes += session.Activity.EstimatedDurationMinutes;

            if (avialableMinutes - estimatedMinutes < 0)
            {
                return Result.Failure<ReviewDoctorSessionResponse>(
                    DoctorSessionReviewErrors.InsufficientCredit);
            }
        }

        session.Activity.CanMakeSession = request.repeatSession;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new ReviewDoctorSessionResponse(
                SessionId: session.Id,
                ChildId: session.ChildId,
                Rating: request.Rating,
                Comment: trimmedComment,
                IsDoctorReviewed: true,
                ReviewedAtUtc: reviewedAtUtc));
    }
}
