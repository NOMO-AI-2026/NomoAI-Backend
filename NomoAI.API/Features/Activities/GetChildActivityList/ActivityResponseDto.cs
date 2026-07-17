using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Activities.GetChildActivityList
{
    public class ActivityResponseDto
    {
        public int Id { get; set; }
        public ActivityTargetType ActivityTarget { get; set; }
        public string Content { get; set; } = string.Empty;
        public int EstimatedDurationMinutes { get; set; }
    }
}
