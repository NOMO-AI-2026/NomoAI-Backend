using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;

namespace NomoAI.API.Features.DoctorNotes.GetAllChildNotes
{
    public class GetAllNotesEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/children/{childId:int}/notes", async Task<IResult> (int childId, int? pageNumber, int? pageSize, IMediator mediator) =>
            {
                var query = new GetAllNotesQuery(childId, pageNumber ?? 1 , pageSize ??10 );
                var result = await mediator.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .WithName("GetAllChildNotes")
            .WithTags("DoctorNotes")
            .WithSummary("Get all notes for a child")
            .Produces<PaginatedList<GetAllNotesResponse>>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
