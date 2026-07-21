using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Profile.GetUserProfile
{
    public class GetUserProfileEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/me/profile", async (ClaimsPrincipal user, IMediator mediator) =>
            {
                string? userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                GetUserProfileQuery query = new GetUserProfileQuery(userId);
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("Profile")
            .WithTags("Profile")
            .WithSummary("Get current user's profile");
        }
    }
}
