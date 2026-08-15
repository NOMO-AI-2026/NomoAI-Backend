namespace NomoAI.API.Features.Profile.UpdateDoctorDocuments;

public sealed class UpdateDoctorDocumentsResponse
{
    public string? PracticeLicenseUrl { get; init; }

    public string? SyndicateCardUrl { get; init; }

    public string? PendingPracticeLicenseUrl { get; init; }

    public string? PendingSyndicateCardUrl { get; init; }

    public bool HasPendingDocuments { get; init; }

    public bool IsApproved { get; init; }
}
