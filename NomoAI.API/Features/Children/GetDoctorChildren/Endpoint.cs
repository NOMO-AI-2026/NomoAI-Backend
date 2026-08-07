using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Features.Children.GetDoctorChildren;
using System.Security.Claims;

namespace NomoAI.API.Features.Children.GetDoctorChildren
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            /// <param name="Gender">Male =0 , Female =1</param>
            /// <param name="Role">Doctor =0 , Parent =1</param>
            app.MapGet("/api/Doctor/Children",
                async (string? Name, int? pageNumber, int? pageSize, IMediator mediator, ClaimsPrincipal user, CancellationToken cancellationToken) =>
                {
                    string UserID = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrWhiteSpace(UserID))
                    {
                        return Results.Unauthorized();
                    }

                    int pn = pageNumber.GetValueOrDefault(1);
                    int ps = pageSize.GetValueOrDefault(10);
                    if (pn <= 0) pn = 1;
                    if (ps <= 0) ps = 10;

                    var query = new GetDoctorChildrenQuery(UserID, Name, pn, ps);
                    var children = await mediator.Send(query, cancellationToken);
                    return children.IsSuccess ? Results.Ok(children.Value) : children.ToProblem();
                })
                .WithTags("Children")
                .WithName("GetDoctorChildren")
                .WithSummary("Get children for a doctor")
                .WithDescription("Returns children associated with the specified doctor.")
                .RequireAuthorization(policy => policy.RequireRole("Doctor"));
        }

    }
}
