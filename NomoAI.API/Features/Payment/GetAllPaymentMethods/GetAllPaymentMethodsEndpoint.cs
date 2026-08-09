using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment.GetAllPaymentMethods
{
    public class GetAllPaymentMethodsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/payment-methods", async Task<IResult> (
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetAllPaymentMethodsQuery(),
                    cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(result)
                    : result.ToProblem();
            })
            .AllowAnonymous()
            .WithName("GetAllPaymentMethods")
            .WithTags("Payment")
            .WithSummary("Get all payment methods")
            .Produces<Result<IEnumerable<PaymentMethodResponse>>>(StatusCodes.Status200OK);
        }
    }
}
