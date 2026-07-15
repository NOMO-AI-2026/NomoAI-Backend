using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;