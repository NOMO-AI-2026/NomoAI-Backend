using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ConfirmEmail;

public sealed record ConfirmEmailCommand(string UserId, string Token) : IRequest<Result>;