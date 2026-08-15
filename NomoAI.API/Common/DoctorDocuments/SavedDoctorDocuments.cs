namespace NomoAI.API.Common.DoctorDocuments;

public sealed record SavedDoctorDocuments(
    string FolderPath,
    string IdentityDocumentUrl,
    string PracticeLicenseUrl,
    string SyndicateCardUrl);
