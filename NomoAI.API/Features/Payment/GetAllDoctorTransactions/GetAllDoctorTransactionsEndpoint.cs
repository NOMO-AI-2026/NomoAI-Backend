using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.Payment.GetAllDoctorTransactions
{
    public class GetAllDoctorTransactionsEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/payments/my-transactions", async Task<IResult> (
                ClaimsPrincipal user,
                int? pageNumber,
                int? pageSize,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var doctorUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(doctorUserId))
                {
                    return Results.Unauthorized();
                }

                var result = await mediator.Send(
                    new GetAllDoctorTransactionsQuery(
                        doctorUserId,
                        pageNumber is null or <= 0 ? 1 : pageNumber.Value,
                        pageSize is null or <= 0 ? 10 : pageSize.Value),
                    cancellationToken);

                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Doctor"))
            .WithName("GetAllDoctorTransactions")
            .WithTags("Payment")
            .WithSummary("Get the authenticated doctor's transactions (paginated)")
            .Produces<Result<PaginatedList<DoctorTransactionResponse>>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
