using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/login", async (LoginCommand command, ISender sender) =>
            {
                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Ok(result)
                    : result.ToProblem();
            })
        .WithName("Login")
        .WithTags("Authentication")
        .Produces<Result<LoginResponseDto>>(StatusCodes.Status200OK)
        .Produces<Result<LoginResponseDto>>(StatusCodes.Status400BadRequest);
        }
    }
}
