using MediatR;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Auth.ForgotPassword;

public static class ForgotPasswordEndpoint
{
	public static void MapEndpoint(RouteGroupBuilder group)
	{
		group
			.MapPost("/forgot-password", HandleAsync)
			.AllowAnonymous()
			.WithName("ForgotPassword")
			.WithSummary("Send a password reset link")
			.WithDescription(
				"Sends a password reset link if the email belongs to an existing account.")
			.Produces<ForgotPasswordResponse>(
				StatusCodes.Status200OK)
			.Produces<Error>(
				StatusCodes.Status400BadRequest);
	}

	private static async Task<IResult> HandleAsync(ForgotPasswordCommand command,ISender sender,CancellationToken cancellationToken)
	{
		var result = await sender.Send(command,cancellationToken);

		if (result.IsFailure)
		{
			return Results.BadRequest(result.Error);
		}

		return Results.Ok(new ForgotPasswordResponse("If an account exists for this email, "+"a password reset link has been sent."));
	}
}

public sealed record ForgotPasswordResponse(string Message);