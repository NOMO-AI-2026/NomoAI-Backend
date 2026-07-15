using NomoAI.API.Features.Auth.ForgotPassword;
using NomoAI.API.Features.Auth.Login_User;
using NomoAI.API.Features.Auth.ResetPassword;

namespace NomoAI.API.Features.Auth;

public static class AuthEndpoints
{
	public static IEndpointRouteBuilder MapAuthEndpoints(
		this IEndpointRouteBuilder app)
	{
		var authGroup = app
			.MapGroup("/api/auth")
			.WithTags("Authentication");

		//LoginEndpoint.MapEndpoint(authGroup);
		ForgotPasswordEndpoint.MapEndpoint(authGroup);
		ResetPasswordEndpoint.MapEndpoint(authGroup);

		// Add the other authentication endpoints here.
		// RegisterEndpoint.MapEndpoint(authGroup);
		// ConfirmEmailEndpoint.MapEndpoint(authGroup);
		// ResendEmailConfirmationEndpoint.MapEndpoint(authGroup);

		return app;
	}
}