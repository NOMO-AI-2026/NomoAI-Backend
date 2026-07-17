using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Activities.CreateActivity;

public sealed record CreateActivityResponse(
    int ActivityId,
    int ChildId,
    ActivityTargetType ActivityTarget,
    string Content,
    int EstimatedDurationMinutes,
    DateTime CreatedAt,
    string Message);