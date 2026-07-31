using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Support.CreateSupportTicket;

public class CreateSupportTicketEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/support/tickets", HandleAsync)
            .RequireAuthorization(policy =>
                policy.RequireRole("Doctor", "Parent"))
            .WithName("CreateSupportTicket")
            .WithTags("Support")
            .WithSummary("Submit a support ticket")
            .WithDescription(
                "Allows an authenticated doctor or parent to submit a support complaint/ticket.")
            .Accepts<CreateSupportTicketRequest>("application/json")
            .Produces<CreateSupportTicketResponse>(
                StatusCodes.Status201Created)
            .Produces<Error>(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> HandleAsync(
        CreateSupportTicketRequest request,
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

        var command = new CreateSupportTicketCommand(
            userId,
            request.Subject,
            request.Message);

        Result<CreateSupportTicketResponse> result =
            await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created(
                $"/api/support/tickets/{result.Value.Id}",
                result.Value)
            : result.ToProblem();
    }
}
