using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ResetPassword;

public static class ResetPasswordEndpoint
{
	public static void MapEndpoint(RouteGroupBuilder group)
	{
		group
			.MapPost("/reset-password", HandleAsync)
			.AllowAnonymous()
			.WithName("ResetPassword")
			.WithSummary("Reset the user's password")
			.WithDescription(
				"Validates the reset token and changes the user's password.")
			.Produces<ResetPasswordResponse>(
				StatusCodes.Status200OK)
			.Produces<Error>(
				StatusCodes.Status400BadRequest);
	}

	private static async Task<IResult> HandleAsync(ResetPasswordCommand command,ISender sender,CancellationToken cancellationToken)
	{
		var result = await sender.Send(command,cancellationToken);

		if (result.IsFailure)
		{
			return Results.BadRequest(result.Error);
		}

		return Results.Ok(new ResetPasswordResponse("Your password has been reset successfully."));
	}
}

public sealed record ResetPasswordResponse(string Message);