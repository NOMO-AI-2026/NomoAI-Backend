using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Activities.CreateActivity;

public sealed record CreateActivityRequest(
    int ChildId,
    ActivityTargetType ActivityTarget,
    string Content,
    int EstimatedDurationMinutes);