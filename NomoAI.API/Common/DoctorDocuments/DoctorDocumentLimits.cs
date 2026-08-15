namespace NomoAI.API.Common.DoctorDocuments;

public static class DoctorDocumentLimits
{
    public const int MaxUrlLength = 1000;
    public const int MaxSyndicateRegistrationNumberLength = 100;
    public const long MaxFileBytes = 5 * 1024 * 1024;
    public const string RelativeFolder = "uploads/doctor-documents";

    public static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "application/pdf"
    ];

    public static readonly string[] AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".pdf"
    ];
}
