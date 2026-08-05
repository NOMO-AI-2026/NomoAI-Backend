using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;
using NomoAI.API.Persistence;
using Microsoft.EntityFrameworkCore;

namespace NomoAI.API.Features.Children.GetChildDetails
{
    public class GetChildDetailsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/children/{childId:int}", async (int childId, ClaimsPrincipal user, IMediator mediator) =>
            {
                var UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = user.FindFirst(ClaimTypes.Role)?.Value;

                var query = new GetChildDeatilsQuery(UserId, childId, role);
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("GetChildDetails")
            .WithTags("Children")
            .WithSummary("Get details for a specific child");
        }
    }
}
