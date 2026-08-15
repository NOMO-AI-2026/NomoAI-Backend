using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.DoctorDocuments;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.Profile.UpdateDoctorDocuments;

internal sealed class UpdateDoctorDocumentsHandler
    : IRequestHandler<UpdateDoctorDocumentsCommand, Result<UpdateDoctorDocumentsResponse>>
{
    private readonly AppDbContext _db;
    private readonly DoctorDocumentStorage _documentStorage;

    public UpdateDoctorDocumentsHandler(
        AppDbContext db,
        DoctorDocumentStorage documentStorage)
    {
        _db = db;
        _documentStorage = documentStorage;
    }

    public async Task<Result<UpdateDoctorDocumentsResponse>> Handle(
        UpdateDoctorDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        var doctor = await _db.Doctor
            .Where(item => item.UserId == request.UserId && !item.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (doctor is null)
        {
            return Result.Failure<UpdateDoctorDocumentsResponse>(
                ProfileErrors.DoctorNotFound);
        }

        string? previousPendingFolder =
            _documentStorage.TryResolveLocalFolderPath(doctor.PendingPracticeLicenseUrl)
            ?? _documentStorage.TryResolveLocalFolderPath(doctor.PendingSyndicateCardUrl);

        Result<SavedDoctorDocuments> saveResult = await _documentStorage.SaveAsync(
            request.PracticeLicense!,
            request.SyndicateCard!,
            request.PublicApiBaseUrl,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            return Result.Failure<UpdateDoctorDocumentsResponse>(saveResult.Error);
        }

        SavedDoctorDocuments saved = saveResult.Value;

        try
        {
            doctor.PendingPracticeLicenseUrl = saved.PracticeLicenseUrl;
            doctor.PendingSyndicateCardUrl = saved.SyndicateCardUrl;

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _documentStorage.TryDeleteFolder(saved.FolderPath);
            throw;
        }

        TryDeletePreviousPendingFolder(previousPendingFolder, saved.FolderPath);

        return Result.Success(
            new UpdateDoctorDocumentsResponse
            {
                PracticeLicenseUrl = doctor.PracticeLicenseUrl,
                SyndicateCardUrl = doctor.SyndicateCardUrl,
                PendingPracticeLicenseUrl = doctor.PendingPracticeLicenseUrl,
                PendingSyndicateCardUrl = doctor.PendingSyndicateCardUrl,
                HasPendingDocuments = true,
                IsApproved = doctor.IsApproved
            });
    }

    private void TryDeletePreviousPendingFolder(string? previousFolder, string newFolder)
    {
        if (string.IsNullOrWhiteSpace(previousFolder)
            || string.Equals(previousFolder, newFolder, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _documentStorage.TryDeleteFolder(previousFolder);
    }
}
