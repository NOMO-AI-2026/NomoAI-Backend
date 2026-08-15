namespace NomoAI.API.Features.AdminDashboard;

public sealed class DoctorDocumentsReviewResponse
{
    public required string UserId { get; init; }

    public bool IsApproved { get; init; }

    public string? PracticeLicenseUrl { get; init; }

    public string? SyndicateCardUrl { get; init; }

    public string? PendingPracticeLicenseUrl { get; init; }

    public string? PendingSyndicateCardUrl { get; init; }

    public bool HasPendingDocuments { get; init; }
}
