using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorDashboard.ReviewDoctorSession;

public sealed record ReviewDoctorSessionCommand(
    int SessionId,
    string DoctorUserId,
    int Rating,
    string Comment,
    bool repeatSession)
    : IRequest<Result<ReviewDoctorSessionResponse>>;
