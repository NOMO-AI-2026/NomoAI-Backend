using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.AcceptDoctorDocuments;

public sealed record AcceptDoctorDocumentsCommand(string UserId)
    : IRequest<Result<DoctorDocumentsReviewResponse>>;
