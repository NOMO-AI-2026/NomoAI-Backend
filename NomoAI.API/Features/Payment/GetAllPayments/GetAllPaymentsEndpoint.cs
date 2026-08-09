using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.Payment.GetAllPayments
{
    public class GetAllPaymentsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/payments", async Task<IResult> (
                int? pageNumber,
                int? pageSize,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAllPaymentsQuery(
                    pageNumber is null or <= 0 ? 1 : pageNumber.Value,
                    pageSize is null or <= 0 ? 10 : pageSize.Value);

                var result = await mediator.Send(query, cancellationToken);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("GetAllPayments")
            .WithTags("Payment")
            .WithSummary("Get all payments for admin (paginated)")
            .Produces<Result<PaginatedList<PaymentListItemResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
        }
    }
}
