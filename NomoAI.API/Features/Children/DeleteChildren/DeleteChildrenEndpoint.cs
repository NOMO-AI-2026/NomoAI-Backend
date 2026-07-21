using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Children.DeleteChildren
{
    public class DeleteChildrenEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/children/{ChildId}", async (int childId , ClaimsPrincipal user, IMediator mediator) =>
            {


                var command = new DeleteChildrenCommand(childId);
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("DeleteMyChild")
            .WithTags("Children")
            .WithSummary("Delete current child (mark as deleted)")
            .RequireAuthorization(policy => policy.RequireRole("Doctor"));
        }
    }
}
