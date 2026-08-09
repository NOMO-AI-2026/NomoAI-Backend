using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment.PaymobWebhook
{
    public class PaymobWebhookEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/payments/paymob/webhook", async Task<IResult> (
                HttpRequest httpRequest,
                PaymobTransactionCallbackDto payload,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var hmac = httpRequest.Query["hmac"].FirstOrDefault();

                var result = await mediator.Send(
                    new PaymobWebhookCommand(hmac, payload),
                    cancellationToken);

                return result.IsSuccess ? Results.Ok() : result.ToProblem();
            })
            .AllowAnonymous()
            .WithName("PaymobWebhook")
            .WithTags("Payment")
            .WithSummary("PayMob Transaction Processed webhook callback")
            .Accepts<PaymobTransactionCallbackDto>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
