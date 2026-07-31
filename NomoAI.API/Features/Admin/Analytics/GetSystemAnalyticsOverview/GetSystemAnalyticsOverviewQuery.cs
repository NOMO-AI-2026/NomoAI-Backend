using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Admin.Analytics.GetSystemAnalyticsOverview;

public sealed record GetSystemAnalyticsOverviewQuery
    : IRequest<Result<GetSystemAnalyticsOverviewResponse>>;
