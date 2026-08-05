using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorDashboard.GetDoctorDashboard;

public sealed record GetDoctorDashboardQuery(string DoctorUserId)
    : IRequest<Result<GetDoctorDashboardResponse>>;
