using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.Jwt;

namespace NomoAI.API.Features.Auth.Login_User
{
    public class LoginEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/login", HandleAsync)
        .WithName("Login")
        .WithTags("Authentication")
        .Produces<Result<LoginResponseDto>>(StatusCodes.Status200OK)
        .Produces<Result<LoginResponseDto>>(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> HandleAsync(
            LoginCommand command,
            ISender sender,
            HttpContext httpContext,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                return result.ToProblem();
            }

            LoginAuthResult authResult = result.Value;

            RefreshTokenCookie.Append(
                httpContext.Response,
                environment,
                authResult.RawRefreshToken,
                authResult.RefreshTokenExpiresAtUtc);

            return Results.Ok(
                Result.Success(authResult.Response));
        }
    }
}
