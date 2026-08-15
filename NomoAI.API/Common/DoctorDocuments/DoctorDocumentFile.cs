namespace NomoAI.API.Common.DoctorDocuments;

public sealed record DoctorDocumentFile(
    Stream Stream,
    string FileName,
    string ContentType,
    long Length);
