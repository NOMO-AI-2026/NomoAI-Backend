using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Profile.DeleteAccount
{
    public class DeleteAccountEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/me/account", async Task<IResult> (ClaimsPrincipal user, IMediator mediator) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = user.FindFirst(ClaimTypes.Role)?.Value;
                var command = new DeleteAccountCommand(userId, role);
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("DeleteMyAccount")
            .WithTags("Profile")
            .WithSummary("Delete the current user account (soft delete)");
        }
    }
}
