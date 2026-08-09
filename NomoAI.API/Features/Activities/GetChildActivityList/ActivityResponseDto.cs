using System.Text.Json.Serialization;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Activities.GetChildActivityList
{
    public class ActivityResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("activityTarget")]
        public ActivityTargetType ActivityTarget { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("estimatedDurationMinutes")]
        public int EstimatedDurationMinutes { get; set; }

        /// <summary>True when the activity may still be used to start a session.</summary>
        [JsonPropertyName("canMakeSession")]
        public bool CanMakeSession { get; set; }
    }
}
