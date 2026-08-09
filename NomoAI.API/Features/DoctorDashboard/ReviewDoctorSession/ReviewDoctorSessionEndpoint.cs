using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.DoctorDashboard.ReviewDoctorSession;

public sealed class ReviewDoctorSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(
                "/api/doctor/sessions/{sessionId:int}/review",
                HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor"))
            .WithName("ReviewDoctorSession")
            .WithTags("Sessions")
            .WithSummary("Review a completed session")
            .WithDescription(
                "Allows an approved doctor to submit a rating and comment " +
                "for a completed session they own, clearing it from awaiting review.")
            .Accepts<ReviewDoctorSessionRequest>("application/json")
            .Produces<ReviewDoctorSessionResponse>(
                StatusCodes.Status200OK)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden)
            .Produces<Error>(
                StatusCodes.Status404NotFound)
            .Produces<Error>(
                StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        int sessionId,
        ReviewDoctorSessionRequest request,
        ClaimsPrincipal currentUser,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        Result<ReviewDoctorSessionResponse> result =
            await sender.Send(
                new ReviewDoctorSessionCommand(
                    sessionId,
                    userId,
                    request.Rating,
                    request.Comment,
                    request.repeatSession),
                cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblem();
    }
}
