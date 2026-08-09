using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Payment.PaymentQuickLink
{
    public class PaymentQuickLinkEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/payments/quick-link", async Task<IResult> (
                PaymentQuickLinkRequest request,
                ClaimsPrincipal user,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var doctorUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(doctorUserId))
                {
                    return Results.Unauthorized();
                }

                var command = new PaymentQuickLinkCommand(
                    doctorUserId,
                    request.PaymentMethodId,
                    request.PlanId,
                    request.Idempotency,
                    request.PriceInEGP);

                var result = await mediator.Send(command, cancellationToken);
                if (result.IsFailure)
                {
                    return result.ToProblem();
                }

                if (result.Value.IsReplay)
                {
                    return Results.Ok(result);
                }

                return Results.Created(
                    $"/api/payments/{result.Value.PaymentId}",
                    result);
            })
            .RequireAuthorization(policy => policy.RequireRole("Doctor"))
            .WithName("CreatePaymentQuickLink")
            .WithTags("Payment")
            .WithSummary("Create a PayMob quick link for a subscription plan purchase")
            .Accepts<PaymentQuickLinkRequest>("application/json")
            .Produces<Result<PaymentQuickLinkResponse>>(StatusCodes.Status201Created)
            .Produces<Result<PaymentQuickLinkResponse>>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound)
            .Produces<Error>(StatusCodes.Status502BadGateway);
        }
    }
}
