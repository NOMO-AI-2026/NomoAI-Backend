namespace NomoAI.API.Common.DoctorDocuments;

public static class DoctorDocumentPublicUrl
{
    public static string BuildRelativePath(string folderId, string fileName) =>
        $"/{DoctorDocumentLimits.RelativeFolder}/{folderId}/{fileName}";

    public static string Build(string relativePath, string? publicApiBaseUrl)
    {
        string path = relativePath.StartsWith('/')
            ? relativePath
            : "/" + relativePath;

        if (string.IsNullOrWhiteSpace(publicApiBaseUrl))
        {
            return path;
        }

        return $"{publicApiBaseUrl.TrimEnd('/')}{path}";
    }
}
