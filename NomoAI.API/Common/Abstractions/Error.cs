namespace NomoAI.API.Common.Abstractions
{
    public record Error(
        string Code,
        string Description,
        int? StatusCode,
        string? CorrelationId = null)
    {
        public static readonly Error None = new(string.Empty, string.Empty, null);
    }

}