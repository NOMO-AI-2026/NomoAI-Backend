using NomoAI.API.Features.Sessions.Runtime;

namespace NomoAI.API.Tests;

public class AttemptAudioUrlTests
{
    [Fact]
    public void BuildRelativePath_Uses_Stable_Api_Route()
    {
        Assert.Equal("/api/sessions/20/attempts/7/audio", AttemptAudioUrl.BuildRelativePath(20, 7));
    }

    [Fact]
    public void Build_Without_Public_Base_Returns_Relative_Path()
    {
        Assert.Equal("/api/sessions/3/attempts/9/audio", AttemptAudioUrl.Build(3, 9, publicApiBaseUrl: null));
        Assert.Equal("/api/sessions/3/attempts/9/audio", AttemptAudioUrl.Build(3, 9, publicApiBaseUrl: "  "));
    }

    [Fact]
    public void Build_With_Public_Base_Prefixes_Absolute_Url()
    {
        Assert.Equal(
            "https://nomoai.runasp.net/api/sessions/3/attempts/9/audio",
            AttemptAudioUrl.Build(3, 9, "https://nomoai.runasp.net/"));
    }

    [Fact]
    public void Different_Attempts_Produce_Different_Urls()
    {
        string first = AttemptAudioUrl.BuildRelativePath(10, 1);
        string second = AttemptAudioUrl.BuildRelativePath(10, 2);

        Assert.NotEqual(first, second);
        Assert.Contains("/attempts/1/", first);
        Assert.Contains("/attempts/2/", second);
    }
}

public class SessionAttemptAudioPersistenceModelTests
{
    [Fact]
    public void SessionAttempts_Can_Hold_Child_Audio_Bytes_And_Url()
    {
        byte[] bytes = [1, 2, 3, 4];
        var attempt = new NomoAI.API.Domain.Entities.SessionAttempts
        {
            SessionId = 5,
            AttemptNumber = 1,
            AudioContent = bytes,
            AudioContentType = "audio/webm",
            AudioUrl = AttemptAudioUrl.BuildRelativePath(5, 42)
        };

        Assert.NotNull(attempt.AudioContent);
        Assert.Equal(4, attempt.AudioContent!.Length);
        Assert.Equal("audio/webm", attempt.AudioContentType);
        Assert.Equal("/api/sessions/5/attempts/42/audio", attempt.AudioUrl);
    }

    [Fact]
    public void SessionAttemptItemResponse_Exposes_AudioUrl_Without_Duplicate_Property_Names()
    {
        var item = new NomoAI.API.Features.Sessions.Runtime.GetSessionAttempts.SessionAttemptItemResponse
        {
            AttemptId = 1,
            AttemptNumber = 1,
            AudioUrl = "/api/sessions/1/attempts/1/audio",
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal("/api/sessions/1/attempts/1/audio", item.AudioUrl);
        Assert.Single(
            typeof(NomoAI.API.Features.Sessions.Runtime.GetSessionAttempts.SessionAttemptItemResponse)
                .GetProperties(),
            p => p.Name.Equals("AudioUrl", StringComparison.Ordinal));
    }
}
