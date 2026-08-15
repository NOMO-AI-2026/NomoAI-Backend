using MediatR;
using Microsoft.EntityFrameworkCore;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.DoctorDocuments;
using NomoAI.API.Domain.Entities;
using NomoAI.API.Persistence;

namespace NomoAI.API.Features.AdminDashboard.RejectDoctorDocuments;

internal sealed class RejectDoctorDocumentsHandler
    : IRequestHandler<RejectDoctorDocumentsCommand, Result<DoctorDocumentsReviewResponse>>
{
    private readonly AppDbContext _db;
    private readonly DoctorDocumentStorage _documentStorage;

    public RejectDoctorDocumentsHandler(
        AppDbContext db,
        DoctorDocumentStorage documentStorage)
    {
        _db = db;
        _documentStorage = documentStorage;
    }

    public async Task<Result<DoctorDocumentsReviewResponse>> Handle(
        RejectDoctorDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        Doctor? doctor = await _db.Doctor
            .Where(item => item.UserId == request.UserId && !item.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken);

        if (doctor is null)
        {
            return Result.Failure<DoctorDocumentsReviewResponse>(
                AdminDashboardErrors.DoctorNotFound);
        }

        if (!HasPendingDocuments(doctor))
        {
            return Result.Failure<DoctorDocumentsReviewResponse>(
                AdminDashboardErrors.NoPendingDocuments);
        }

        string? pendingFolder =
            _documentStorage.TryResolveLocalFolderPath(doctor.PendingPracticeLicenseUrl)
            ?? _documentStorage.TryResolveLocalFolderPath(doctor.PendingSyndicateCardUrl);

        doctor.PendingPracticeLicenseUrl = null;
        doctor.PendingSyndicateCardUrl = null;

        await _db.SaveChangesAsync(cancellationToken);

        _documentStorage.TryDeleteFolder(pendingFolder);

        return Result.Success(Map(doctor));
    }

    private static bool HasPendingDocuments(Doctor doctor) =>
        !string.IsNullOrWhiteSpace(doctor.PendingPracticeLicenseUrl)
        || !string.IsNullOrWhiteSpace(doctor.PendingSyndicateCardUrl);

    private static DoctorDocumentsReviewResponse Map(Doctor doctor) =>
        new()
        {
            UserId = doctor.UserId,
            IsApproved = doctor.IsApproved,
            PracticeLicenseUrl = doctor.PracticeLicenseUrl,
            SyndicateCardUrl = doctor.SyndicateCardUrl,
            PendingPracticeLicenseUrl = doctor.PendingPracticeLicenseUrl,
            PendingSyndicateCardUrl = doctor.PendingSyndicateCardUrl,
            HasPendingDocuments = false
        };
}
