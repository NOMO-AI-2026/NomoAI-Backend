using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ConfirmEmailChange;

public sealed record ConfirmEmailChangeCommand(
    string UserId,
    string Otp)
    : IRequest<Result>;