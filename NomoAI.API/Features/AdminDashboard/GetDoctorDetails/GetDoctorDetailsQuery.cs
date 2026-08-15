using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails
{
    public record GetDoctorDetailsQuery(string UserId)
        : IRequest<Result<DoctorDetailsResponse>>;
}
