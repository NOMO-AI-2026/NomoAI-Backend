using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.GetAllDoctors
{
    public record GetAllDoctorsQuery(bool IsApproved, int PageNumber, int PageSize) : IRequest<Result<PaginatedList<DoctorResponse>>>;
}
