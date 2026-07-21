namespace NomoAI.API.Common.Redis;

public sealed class UpstashRedisOptions
{
    public const string SectionName =
        "UpstashRedis";

    public string Endpoint { get; init; }
        = string.Empty;

    public int Port { get; init; } = 6379;

    public string User { get; init; }
        = "default";

    public string Password { get; init; }
        = string.Empty;

    public bool UseSsl { get; init; } = true;

    public string InstancePrefix { get; init; }
        = "nomoai:dev:";
}