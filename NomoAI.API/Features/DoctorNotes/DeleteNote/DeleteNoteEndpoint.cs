using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.DoctorNotes.DeleteNote
{
    public class DeleteNoteEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/notes/{noteId:int}", async Task<IResult> (int noteId, ClaimsPrincipal user, IMediator mediator) =>
            {
                var doctorUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(doctorUserId))
                {
                    return Results.Unauthorized();
                }
                var command = new DeleteNoteQuery(noteId, doctorUserId);
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok() : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Doctor"))
            .WithName("DeleteNote")
            .WithTags("DoctorNotes")
            .WithSummary("Delete a doctor's note ")
            .Produces(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
