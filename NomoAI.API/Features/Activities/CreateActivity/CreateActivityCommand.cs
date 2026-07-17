using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Activities.CreateActivity;

public sealed record CreateActivityCommand(
    int ChildId,
    ActivityTargetType ActivityTarget,
    string Content,
    int EstimatedDurationMinutes,
    string DoctorUserId)
    : IRequest<Result<CreateActivityResponse>>;