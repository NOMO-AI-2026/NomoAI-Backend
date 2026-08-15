using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails;

public sealed record GetDoctorDetailsQuery(string UserId)
    : IRequest<Result<DoctorDetailsResponse>>;
