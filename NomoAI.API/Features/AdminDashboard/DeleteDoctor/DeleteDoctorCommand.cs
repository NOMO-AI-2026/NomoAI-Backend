using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.DeleteDoctor
{
    public record DeleteDoctorCommand(string UserId) : IRequest<Result>;
}
