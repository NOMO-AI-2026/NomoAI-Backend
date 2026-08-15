using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.RejectDoctorDocuments;

public sealed record RejectDoctorDocumentsCommand(string UserId)
    : IRequest<Result<DoctorDocumentsReviewResponse>>;
