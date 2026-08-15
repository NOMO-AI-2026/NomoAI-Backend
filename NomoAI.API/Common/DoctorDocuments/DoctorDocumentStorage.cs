using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Common.DoctorDocuments;

public sealed class DoctorDocumentStorage
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DoctorDocumentStorage> _logger;

    public DoctorDocumentStorage(
        IWebHostEnvironment environment,
        ILogger<DoctorDocumentStorage> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<Result<SavedDoctorDocuments>> SaveAsync(
        DoctorDocumentFile identityDocument,
        DoctorDocumentFile practiceLicense,
        DoctorDocumentFile syndicateCard,
        string publicApiBaseUrl,
        CancellationToken cancellationToken)
    {
        string webRoot = string.IsNullOrWhiteSpace(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        string folderId = Guid.NewGuid().ToString("N");
        string folderPath = Path.Combine(
            webRoot,
            "uploads",
            "doctor-documents",
            folderId);

        try
        {
            Directory.CreateDirectory(folderPath);

            string identityFileName = await SaveFileAsync(
                identityDocument,
                folderPath,
                "identity",
                cancellationToken);

            string licenseFileName = await SaveFileAsync(
                practiceLicense,
                folderPath,
                "practice-license",
                cancellationToken);

            string syndicateFileName = await SaveFileAsync(
                syndicateCard,
                folderPath,
                "syndicate-card",
                cancellationToken);

            return Result.Success(
                new SavedDoctorDocuments(
                    folderPath,
                    DoctorDocumentPublicUrl.Build(
                        DoctorDocumentPublicUrl.BuildRelativePath(folderId, identityFileName),
                        publicApiBaseUrl),
                    DoctorDocumentPublicUrl.Build(
                        DoctorDocumentPublicUrl.BuildRelativePath(folderId, licenseFileName),
                        publicApiBaseUrl),
                    DoctorDocumentPublicUrl.Build(
                        DoctorDocumentPublicUrl.BuildRelativePath(folderId, syndicateFileName),
                        publicApiBaseUrl)));
        }
        catch (OperationCanceledException)
        {
            TryDeleteFolder(folderPath);
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to save doctor verification documents to {FolderPath}.",
                folderPath);

            TryDeleteFolder(folderPath);

            return Result.Failure<SavedDoctorDocuments>(
                DoctorDocumentErrors.SaveFailed);
        }
    }

    public void TryDeleteFolder(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        try
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, recursive: true);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not clean up doctor document folder {FolderPath}.",
                folderPath);
        }
    }

    private static async Task<string> SaveFileAsync(
        DoctorDocumentFile file,
        string folderPath,
        string nameWithoutExtension,
        CancellationToken cancellationToken)
    {
        string extension = ResolveExtension(file.FileName, file.ContentType);
        string fileName = nameWithoutExtension + extension;
        string fullPath = Path.Combine(folderPath, fileName);

        await using FileStream output = new(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        await file.Stream.CopyToAsync(output, cancellationToken);
        return fileName;
    }

    private static string ResolveExtension(string fileName, string contentType)
    {
        if (contentType.Contains("png", StringComparison.OrdinalIgnoreCase))
        {
            return ".png";
        }

        if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            return ".pdf";
        }

        if (contentType.Contains("jpeg", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("jpg", StringComparison.OrdinalIgnoreCase))
        {
            return ".jpg";
        }

        string extension = Path.GetExtension(fileName);
        if (extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return ".jpg";
        }

        return extension.ToLowerInvariant();
    }
}
