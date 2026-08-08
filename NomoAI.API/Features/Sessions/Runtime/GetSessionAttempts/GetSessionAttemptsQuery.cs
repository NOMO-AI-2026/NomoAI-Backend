using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Sessions.Runtime.GetSessionAttempts;

public sealed record GetSessionAttemptsQuery(int SessionId, string UserId)
    : IRequest<Result<GetSessionAttemptsResponse>>;
