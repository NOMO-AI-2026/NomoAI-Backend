using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.DoctorNotes.UpdateNote
{
    public class UpdateNoteEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("api/notes/{noteId:int}", async Task<IResult> (int noteId, UpdateNoteRequest request, ClaimsPrincipal user, IMediator mediator) =>
            {
                var doctorUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(doctorUserId))
                {
                    return Results.Unauthorized();
                }

                var command = new UpdateNoteCommand(noteId, doctorUserId, request.Title, request.Description);
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Ok(result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Doctor"))
            .WithName("UpdateNote")
            .WithTags("DoctorNotes")
            .WithSummary("Update a doctor's note")
            .Accepts<UpdateNoteRequest>("application/json")
            .Produces(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
