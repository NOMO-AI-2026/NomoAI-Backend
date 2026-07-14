using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MediatR;
namespace NomoAI.API.Features.Auth.Register_User
{
    public class RegisterEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/register",
                async (
                    RegisterRequestDto request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    var command = new RegisterUserCommand
                    {
                        FullName = request.FullName,
                        Username = request.Username,
                        Password = request.Password,
                        Gender = request.Gender,
                        Age = request.Age,
                        Role = request.Role
                    };

                    var result = await mediator.Send(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result)
                        : result.ToProblem();
                })
                .WithName("Register")
                .WithTags("Authentication")
                .Produces<Result<RegisterResponseDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
        }
    }
}
