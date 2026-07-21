using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Features.Auth.Register_User;
using System.Security.Claims;

namespace NomoAI.API.Features.Children.GetDoctorChildren
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            /// <param name="Gender">Male = 0 , Female = 1</param>
            /// <param name="Role">Doctor = 0 , Parent = 1</param>
            app.MapGet("/api/Doctor/Children",
                async (
                    IMediator mediator,
                     ClaimsPrincipal user,
                    CancellationToken cancellationToken) =>
                {
                    string UserID = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var query = new GetDoctorChildrenQuery(UserID);
                    var children = await mediator.Send(query, cancellationToken);
                    return children.IsSuccess ? Results.Ok(children) : children.ToProblem();
                })
                .WithTags("Children")
                .WithName("GetDoctorChildren")
                 .WithSummary("Get children for a doctor")
                 .WithDescription("Returns children associated with the specified doctor.")
                 .RequireAuthorization(policy => policy.RequireRole("Doctor"));
        }

    }
}
