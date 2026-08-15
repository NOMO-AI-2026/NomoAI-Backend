namespace NomoAI.API.Common.DoctorDocuments;

public sealed record SavedDoctorDocuments(
    string FolderPath,
    string PracticeLicenseUrl,
    string SyndicateCardUrl);
