using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.AdminDashboard.GetAllDoctors
{
    public class GerAllApprovedDoctorEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/admin/doctors", async Task<IResult> (
                string? Name,
                bool? isApproved,
                bool? hasPendingDocuments,
                int pageNumber,
                int pageSize,
                IMediator mediator) =>
            {
                var query = new GetAllDoctorsQuery(
                    Name,
                    isApproved,
                    hasPendingDocuments,
                    pageNumber <= 0 ? 1 : pageNumber,
                    pageSize <= 0 ? 10 : pageSize);

                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("GetAllDoctors")
            .WithTags("AdminDashboard")
            .WithSummary("Get doctors list with pagination, approval, and pending-documents filters")
            .WithDescription(
                "Optional filters: Name, isApproved, hasPendingDocuments. " +
                "Use hasPendingDocuments=true to list only doctors with replacement documents awaiting review.")
            .Produces<PaginatedList<DoctorResponse>>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized);
        }
    }
}
