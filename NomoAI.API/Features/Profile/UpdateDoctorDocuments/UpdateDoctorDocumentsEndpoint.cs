using System.Security.Claims;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Options;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.DoctorDocuments;
using NomoAI.API.Common.Options;

namespace NomoAI.API.Features.Profile.UpdateDoctorDocuments;

public sealed class UpdateDoctorDocumentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/profile/doctor-documents", HandleAsync)
            .RequireAuthorization(policy => policy.RequireRole("Doctor"))
            .DisableAntiforgery()
            .WithName("UpdateDoctorDocuments")
            .WithTags("Profile")
            .WithSummary("Submit replacement verification documents for admin review")
            .WithDescription(
                "Doctor only. Upload a new practiceLicense and syndicateCard (JPEG, PNG, or PDF, max 5MB each). " +
                "New files are stored as pending until an admin accepts them. " +
                "Active documents and IsApproved are not changed. " +
                "JSON profile update cannot change these files.")
            .Produces<Result<UpdateDoctorDocumentsResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<Error>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest httpRequest,
        ClaimsPrincipal currentUser,
        IMediator mediator,
        IOptions<PublicApiOptions> publicApiOptions,
        CancellationToken cancellationToken)
    {
        string? userId = currentUser.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        if (!httpRequest.HasFormContentType)
        {
            return Result.Failure(
                    ValidationErrors.FromValidationFailures(
                    [
                        new ValidationFailure(
                            "Request",
                            "Content-Type must be multipart/form-data.")
                    ]))
                .ToProblem();
        }

        IFormCollection form = await httpRequest.ReadFormAsync(cancellationToken);

        string publicBaseUrl = publicApiOptions.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            publicBaseUrl = $"{httpRequest.Scheme}://{httpRequest.Host.Value}";
        }

        var command = new UpdateDoctorDocumentsCommand(
            userId,
            ToFile(form.Files.GetFile("practiceLicense")),
            ToFile(form.Files.GetFile("syndicateCard")),
            publicBaseUrl);

        Result<UpdateDoctorDocumentsResponse> result =
            await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblem();
    }

    private static DoctorDocumentFile? ToFile(IFormFile? file)
    {
        if (file is null)
        {
            return null;
        }

        return new DoctorDocumentFile(
            file.OpenReadStream(),
            Path.GetFileName(file.FileName),
            file.ContentType ?? string.Empty,
            file.Length);
    }
}
