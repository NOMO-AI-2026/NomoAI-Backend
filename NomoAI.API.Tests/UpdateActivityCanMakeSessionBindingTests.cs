using System.Text.Json;
using NomoAI.API.Domain.Enums;
using NomoAI.API.Features.Activities.UpdateActivity;

namespace NomoAI.API.Tests;

public class UpdateActivityCanMakeSessionBindingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Deserializes_CanMakeSession_False_From_CamelCase_Json()
    {
        const string json = """
            {
              "activityTarget": 0,
              "content": "حرف الالف",
              "estimatedDurationMinutes": 1,
              "canMakeSession": false
            }
            """;

        UpdateActivityRequest? request =
            JsonSerializer.Deserialize<UpdateActivityRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.Equal(ActivityTargetType.OneCharacter, request!.ActivityTarget);
        Assert.False(request.CanMakeSession);
    }

    [Fact]
    public void Deserializes_CanMakeSession_True_From_CamelCase_Json()
    {
        const string json = """
            {
              "activityTarget": 1,
              "content": "بابا",
              "estimatedDurationMinutes": 10,
              "canMakeSession": true
            }
            """;

        UpdateActivityRequest? request =
            JsonSerializer.Deserialize<UpdateActivityRequest>(json, JsonOptions);

        Assert.NotNull(request);
        Assert.True(request!.CanMakeSession);
    }
}
