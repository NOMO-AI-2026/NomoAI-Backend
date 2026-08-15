namespace NomoAI.API.Common.DoctorDocuments;

public static class DoctorDocumentFileRules
{
    public static string? Validate(DoctorDocumentFile? file, string fieldDisplayName)
    {
        if (file is null)
        {
            return $"{fieldDisplayName} is required.";
        }

        if (file.Length <= 0 || file.Stream is null)
        {
            return $"{fieldDisplayName} must not be empty.";
        }

        if (file.Length > DoctorDocumentLimits.MaxFileBytes)
        {
            return $"{fieldDisplayName} must not exceed {DoctorDocumentLimits.MaxFileBytes} bytes.";
        }

        if (!HasAllowedType(file))
        {
            return $"{fieldDisplayName} must be a JPEG, PNG, or PDF file.";
        }

        return null;
    }

    public static bool HasAllowedType(DoctorDocumentFile file)
    {
        bool contentTypeMissing =
            string.IsNullOrWhiteSpace(file.ContentType)
            || string.Equals(
                file.ContentType.Trim(),
                "application/octet-stream",
                StringComparison.OrdinalIgnoreCase);

        bool contentTypeAllowed =
            !contentTypeMissing
            && DoctorDocumentLimits.AllowedContentTypes.Contains(
                file.ContentType.Trim(),
                StringComparer.OrdinalIgnoreCase);

        if (contentTypeAllowed)
        {
            return true;
        }

        if (!contentTypeMissing)
        {
            return false;
        }

        string extension = Path.GetExtension(file.FileName);
        return DoctorDocumentLimits.AllowedExtensions.Contains(
            extension,
            StringComparer.OrdinalIgnoreCase);
    }
}
