using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Profile.DeleteAccount
{
    public record DeleteAccountCommand(string UserId, string? Role) : IRequest<Result>;
}
