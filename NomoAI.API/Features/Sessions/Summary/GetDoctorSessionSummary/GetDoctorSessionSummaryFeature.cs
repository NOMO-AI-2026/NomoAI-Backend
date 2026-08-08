using System.Security.Claims;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Features.Sessions.Runtime;
using NomoAI.API.Persistence;
using NomoDoc.Domain.Entities;

namespace NomoAI.API.Features.Sessions.Summary.GetDoctorSessionSummary;

public sealed record GetDoctorSessionSummaryQuery(int SessionId, string UserId)
    : IRequest<Result<DoctorSessionSummaryResponse>>;

public sealed class GetDoctorSessionSummaryQueryValidator : AbstractValidator<GetDoctorSessionSummaryQuery>
{
    public GetDoctorSessionSummaryQueryValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}

internal sealed class GetDoctorSessionSummaryQueryHandler
    : IRequestHandler<GetDoctorSessionSummaryQuery, Result<DoctorSessionSummaryResponse>>
{
    private readonly AppDbContext _db;

    public GetDoctorSessionSummaryQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DoctorSessionSummaryResponse>> Handle(
        GetDoctorSessionSummaryQuery request,
        CancellationToken cancellationToken)
    {
        (ChildOwnershipStatus status, Session? session) =
            await SessionOwnership.CheckSessionOwnershipAsync(
                _db, request.UserId, request.SessionId, cancellationToken);

        if (status == ChildOwnershipStatus.ChildNotFound || session is null)
        {
            return Result.Failure<DoctorSessionSummaryResponse>(SessionSummaryErrors.SessionNotFound);
        }

        if (status == ChildOwnershipStatus.Forbidden)
        {
            return Result.Failure<DoctorSessionSummaryResponse>(SessionSummaryErrors.Forbidden);
        }

        bool isDoctor = await _db.Doctor
            .AsNoTracking()
            .AnyAsync(d => d.UserId == request.UserId && !d.IsDeleted, cancellationToken);

        if (!isDoctor)
        {
            return Result.Failure<DoctorSessionSummaryResponse>(SessionSummaryErrors.Forbidden);
        }

        SessionSummary? summary = await _db.SessionSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.SessionId == session.Id && !s.IsDeleted,
                cancellationToken);

        if (summary is null)
        {
            return Result.Failure<DoctorSessionSummaryResponse>(SessionSummaryErrors.SummaryNotFound);
        }

        string childName = await _db.Children
            .AsNoTracking()
            .Where(c => c.Id == session.ChildId && !c.IsDeleted)
            .Select(c => c.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "الطفل";

        SessionSummaryDto dto = SessionSummaryMapper.ToDto(session, summary, childName);
        return Result.Success(SessionSummaryMapper.ToDoctorResponse(dto));
    }
}

public sealed class GetDoctorSessionSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/doctor/sessions/{sessionId:int}/summary", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor"))
            .WithName("GetDoctorSessionSummary")
            .WithTags("DoctorDashboard")
            .WithSummary("Get a detailed clinical session summary for the owning doctor")
            .Produces<DoctorSessionSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        int sessionId,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<DoctorSessionSummaryResponse> result = await sender.Send(
            new GetDoctorSessionSummaryQuery(sessionId, userId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
