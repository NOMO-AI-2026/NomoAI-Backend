using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Children.GetParentChildren
{
    public class GetParentChildrenEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/parent/children", async Task<IResult> (ClaimsPrincipal user, IMediator mediator) =>
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Results.Unauthorized();
                }

                var query = new GetParentChildrenQuery(userId);
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Parent"))
            .WithName("GetParentChildren")
            .WithTags("Children")
            .WithSummary("Get children for authenticated parent")
            .Produces<IEnumerable<ChildrenResponse>>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
