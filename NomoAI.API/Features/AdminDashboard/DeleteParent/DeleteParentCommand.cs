using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.DeleteParent
{
    public record DeleteParentCommand(string UserId) : IRequest<Result>;
}
