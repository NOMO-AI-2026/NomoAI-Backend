using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Profile.UpdateUserProfile
{
    public class UpdateUserProfileEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/me/profile", async Task<IResult> (ClaimsPrincipal user, UpdateProfileRequest request, IMediator mediator) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var command = new UpdateUserProfileCommand(userId) { Request = request };
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("UpdateMyProfile")
            .WithTags("Profile")
            .WithSummary("Update current user's profile")
            .Produces<bool>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
