using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Common.DoctorDocuments;

public static class DoctorDocumentErrors
{
    public static readonly Error SaveFailed = new(
        "Auth.DoctorDocumentSaveFailed",
        "The verification documents could not be saved. Please try again.",
        StatusCodes.Status500InternalServerError);
}
