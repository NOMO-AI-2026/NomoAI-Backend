using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Activities.UpdateActivity;

public sealed record UpdateActivityResponse(
    int ActivityId,
    int ChildId,
    ActivityTargetType ActivityTarget,
    string Content,
    int EstimatedDurationMinutes,
    bool CanMakeSession,
    string Message);