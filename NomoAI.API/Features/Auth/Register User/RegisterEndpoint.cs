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
            /// <param name="Gender">Male = 0 , Female = 1</param>
            /// <param name="Role">Doctor = 0 , Parent = 1</param>
            app.MapPost("/api/auth/register",
                async (
                    RegisterRequestDto request,
                    IMediator mediator,
                    CancellationToken cancellationToken) =>
                {
                    var command = new RegisterUserCommand
                    {
                        FullName = request.FullName,
                        Email = request.Email,
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
                .WithSummary("Register a new user to the system. Parent or Doctor")
                .WithDescription(
                    "Registers a new user to the system. " +
                    "The user can be either a parent or a doctor, depending on the role specified in the request.\n" + 
                    "Role = 0 => Doctor , Role = 1 => Parent\n"+"Gender = 0 => Male , Gender = 1 => Female")
                .Produces<Result<RegisterResponseDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);
        }
    }
}
