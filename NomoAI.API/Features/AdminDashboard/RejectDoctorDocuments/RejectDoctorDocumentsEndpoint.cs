using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.RejectDoctorDocuments;

public sealed class RejectDoctorDocumentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/doctors/{userId}/documents/reject", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("RejectDoctorDocuments")
            .WithTags("AdminDashboard")
            .WithSummary("Reject pending doctor verification documents")
            .WithDescription(
                "Discards pending practice license and syndicate card replacements. " +
                "Active documents and IsApproved are unchanged. Returns 409 if there are no pending documents.")
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
            await mediator.Send(new RejectDoctorDocumentsCommand(userId), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblem();
    }
}
