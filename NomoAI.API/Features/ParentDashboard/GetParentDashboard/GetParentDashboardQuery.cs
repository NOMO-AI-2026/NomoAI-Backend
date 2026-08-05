using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.ParentDashboard.GetParentDashboard;

public sealed record GetParentDashboardQuery(string ParentUserId)
    : IRequest<Result<GetParentDashboardResponse>>;
