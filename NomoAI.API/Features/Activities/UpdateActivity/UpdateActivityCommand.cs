using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Activities.UpdateActivity;

public sealed record UpdateActivityCommand(
    int ActivityId,
    ActivityTargetType ActivityTarget,
    string Content,
    int EstimatedDurationMinutes,
    bool CanMakeSession,
    string DoctorUserId)
    : IRequest<Result<UpdateActivityResponse>>;
