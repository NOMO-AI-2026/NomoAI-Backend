using MediatR;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using System.Security.Claims;

namespace NomoAI.API.Features.DoctorNotes.AddNote
{
    public class AddNoteEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/children/{childId:int}/notes", async Task<IResult> (int childId, AddNoteRequest request, ClaimsPrincipal user, IMediator mediator) =>
            {
                var doctorUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(doctorUserId))
                {
                    return Results.Unauthorized();
                }

                var command = new AddNoteCommand(childId, doctorUserId, request.NoteTitle, request.NoteContent);
                var result = await mediator.Send(command);
                return result.IsSuccess ? Results.Created($"/children/{childId}/notes/{result.Value}",result) : result.ToProblem();
            })
            .RequireAuthorization(policy => policy.RequireRole("Doctor"))
            .WithName("AddNote")
            .WithTags("DoctorNotes")
            .WithSummary("Add a doctor note for a child")
            .Accepts<AddNoteRequest>("application/json")
            .Produces<int>(StatusCodes.Status201Created)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .Produces<Error>(StatusCodes.Status401Unauthorized)
            .Produces<Error>(StatusCodes.Status404NotFound);
        }
    }
}
