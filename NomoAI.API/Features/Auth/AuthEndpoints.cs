using NomoAI.API.Features.Auth.ChangePassword;
using NomoAI.API.Features.Auth.ConfirmEmail;
using NomoAI.API.Features.Auth.ForgotPassword;
using NomoAI.API.Features.Auth.Login_User;
using NomoAI.API.Features.Auth.ResendEmailConfirmation;
using NomoAI.API.Features.Auth.ResetPassword;
using NomoAI.API.Features.Auth.ChangeEmail;
using NomoAI.API.Features.Auth.ConfirmEmailChange;
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
		ConfirmEmailEndpoint.MapEndpoint(authGroup);
		ResendEmailConfirmationEndpoint.MapEndpoint(authGroup);
        ChangePasswordEndpoint.MapEndpoint(authGroup);
		ChangeEmailEndpoint.MapEndpoint(authGroup);
		ConfirmEmailChangeEndpoint.MapEndpoint(authGroup);

        // Add the other authentication endpoints here.
        // RegisterEndpoint.MapEndpoint(authGroup);
        // ConfirmEmailEndpoint.MapEndpoint(authGroup);
        // ResendEmailConfirmationEndpoint.MapEndpoint(authGroup);

        return app;
	}
}