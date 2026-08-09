namespace NomoAI.API.Features.Payment.PaymentQuickLink
{
    public sealed record PaymentQuickLinkResponse(
        int PaymentId,
        string? ClientUrl,
        string? ShortUrl,
        string ReferenceId,
        DateTime? ExpiresAt,
        bool IsReplay = false);
}
