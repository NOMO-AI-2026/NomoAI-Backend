using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ResendEmailConfirmation;

public sealed record ResendEmailConfirmationCommand(
    string UserId)
    : IRequest<Result>;