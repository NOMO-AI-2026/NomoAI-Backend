using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.Logout;

public sealed record LogoutCommand(
    string? RefreshToken)
    : IRequest<Result>;
