using System.Text.Json.Serialization;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Activities.UpdateActivity;

/// <summary>
/// Explicit DTO (not a positional record) so boolean CanMakeSession
/// binds correctly for both true and false from JSON.
/// </summary>
public sealed class UpdateActivityRequest
{
    [JsonPropertyName("activityTarget")]
    public ActivityTargetType ActivityTarget { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("estimatedDurationMinutes")]
    public int EstimatedDurationMinutes { get; set; }

    /// <summary>
    /// Doctor-controlled. Must bind false correctly — do not default to true
    /// in a way that overrides an explicit false from the client.
    /// </summary>
    [JsonPropertyName("canMakeSession")]
    public bool CanMakeSession { get; set; }
}
