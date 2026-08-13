using MediatR;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Features.Auth.Login_User;

namespace NomoAI.API.Features.Auth.Refresh;

public sealed record RefreshCommand(
    string RefreshToken)
    : IRequest<Result<LoginResponseDto>>;
