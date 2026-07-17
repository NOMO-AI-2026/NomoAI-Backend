using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Children.Add_Child
{
    public class AddChildEndpoint : NomoAI.API.Common.IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/children", async (
                IMediator mediator,
                ClaimsPrincipal user,
                AddChildrenRequestDto request) =>
            {
                var command = new AddChildCommand
                {
                    FullName = request.FullName,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    TherapyStartDate = request.TherapyStartDate,
                    Age = request.Age,
                    SpeechLevelId = request.SpeechLevelId,
                    UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };
                var result = await mediator.Send(command);
                return result.IsSuccess
                    ? Results.Ok(result)
                    : result.ToProblem();
            })
             .WithTags("Children")
            .RequireAuthorization(policy => policy.RequireRole("Doctor"));
        }
    }
}
