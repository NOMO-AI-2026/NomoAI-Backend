using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Children.AssignChildToParent;

public sealed record AssignChildToParentCommand(
    int ChildId,
    int ParentId,
    string DoctorUserId)
    : IRequest<Result<AssignChildToParentResponse>>;