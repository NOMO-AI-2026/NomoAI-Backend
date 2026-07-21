using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ChangeEmail;

public sealed record ChangeEmailCommand(
    string UserId,
    string CurrentPassword,
    string NewEmail)
    : IRequest<Result>;