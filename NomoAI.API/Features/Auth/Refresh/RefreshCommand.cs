using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.Refresh;

public sealed record RefreshCommand(
    string RefreshToken)
    : IRequest<Result<RefreshAuthResult>>;
