using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.AcceptDoctorDocuments;

public sealed class AcceptDoctorDocumentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/doctors/{userId}/documents/accept", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("AcceptDoctorDocuments")
            .WithTags("AdminDashboard")
            .WithSummary("Accept pending doctor verification documents")
            .WithDescription(
                "Promotes pending practice license and syndicate card URLs to active. " +
                "Does not change IsApproved. Returns 409 if there are no pending documents.")
            .Produces<Result<DoctorDocumentsReviewResponse>>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound)
            .Produces<Error>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        string userId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        Result<DoctorDocumentsReviewResponse> result =
            await mediator.Send(new AcceptDoctorDocumentsCommand(userId), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblem();
    }
}
