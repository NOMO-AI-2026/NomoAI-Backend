using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.ToggleDoctorApproval
{
    public record ToggleDoctorApprovalCommand(string UserId, bool ApproveStatus) : IRequest<Result>;
}
