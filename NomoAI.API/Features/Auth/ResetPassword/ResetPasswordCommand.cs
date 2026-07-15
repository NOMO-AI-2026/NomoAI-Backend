using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string UserId,string Token,string NewPassword,string ConfirmPassword): IRequest<Result>;