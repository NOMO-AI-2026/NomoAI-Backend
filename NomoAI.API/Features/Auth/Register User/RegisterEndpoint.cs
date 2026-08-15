using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Options;
using NomoAI.API.Common;
using NomoAI.API.Common.Abstractions;
using NomoAI.API.Common.DoctorDocuments;
using NomoAI.API.Common.Enums;
using NomoAI.API.Common.Options;
using NomoAI.API.Features.Auth;

namespace NomoAI.API.Features.Auth.Register_User;

public class RegisterEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", HandleAsync)
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithName("Register")
            .WithTags("Authentication")
            .WithSummary("Register a Parent (JSON) or Doctor (multipart) account")
            .WithDescription(
                "Parent: POST application/json with the existing account fields. Document files are not required.\n" +
                "Doctor: POST multipart/form-data in a single request with account fields plus " +
                "practiceLicense and syndicateCard (JPEG/PNG/PDF, max 5MB each). " +
                "Role = 0 (Doctor) or 1 (Parent). " +
                "Gender = 0 (Male) or 1 (Female). Doctor JSON-only register is rejected.")
            .Produces<Result<RegisterResponseDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status415UnsupportedMediaType);
    }

    private static async Task<IResult> HandleAsync(
        HttpRequest httpRequest,
        IMediator mediator,
        IOptions<PublicApiOptions> publicApiOptions,
        DoctorDocumentStorage documentStorage,
        CancellationToken cancellationToken)
    {
        string? contentType = httpRequest.ContentType;

        if (IsJson(contentType))
        {
            return await HandleJsonAsync(httpRequest, mediator, cancellationToken);
        }

        if (IsMultipart(contentType))
        {
            return await HandleMultipartAsync(
                httpRequest,
                mediator,
                publicApiOptions.Value.BaseUrl,
                documentStorage,
                cancellationToken);
        }

        return Result.Failure(AuthErrors.UnsupportedRegisterContentType).ToProblem();
    }

    private static async Task<IResult> HandleJsonAsync(
        HttpRequest httpRequest,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        RegisterRequestDto? request;
        try
        {
            request = await httpRequest.ReadFromJsonAsync<RegisterRequestDto>(cancellationToken);
        }
        catch (JsonException)
        {
            return Result.Failure(AuthErrors.InvalidRegisterBody).ToProblem();
        }

        if (request is null)
        {
            return Result.Failure(AuthErrors.InvalidRegisterBody).ToProblem();
        }

        if (request.Role == UserRole.Doctor)
        {
            return Result.Failure(AuthErrors.DoctorMultipartRequired).ToProblem();
        }

        var command = new RegisterUserCommand
        {
            FullName = request.FullName,
            Email = request.Email,
            Password = request.Password,
            Gender = request.Gender,
            Age = request.Age,
            Role = request.Role,
            PhoneNumber = request.PhoneNumber,
            YearsOfExperience = request.YearsOfExperience,
            ClinicName = request.ClinicName,
            ProfessionalBio = request.ProfessionalBio
        };

        Result<RegisterResponseDto> result =
            await mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result)
            : result.ToProblem();
    }

    private static async Task<IResult> HandleMultipartAsync(
        HttpRequest httpRequest,
        IMediator mediator,
        string publicApiBaseUrl,
        DoctorDocumentStorage documentStorage,
        CancellationToken cancellationToken)
    {
        IFormCollection form = await httpRequest.ReadFormAsync(cancellationToken);

        if (!DoctorRegisterFormBinder.TryBind(form, out DoctorRegisterForm? bound, out var failures)
            || bound is null)
        {
            return Result.Failure(ValidationErrors.FromValidationFailures(failures)).ToProblem();
        }

        if (bound.Role != UserRole.Doctor)
        {
            return Result.Failure(
                    bound.Role == UserRole.Parent
                        ? AuthErrors.ParentJsonRequired
                        : AuthErrors.InvalidRegisterBody)
                .ToProblem();
        }

        if (string.IsNullOrWhiteSpace(publicApiBaseUrl))
        {
            publicApiBaseUrl = $"{httpRequest.Scheme}://{httpRequest.Host.Value}";
        }

        Result<SavedDoctorDocuments> saveResult = await documentStorage.SaveAsync(
            bound.PracticeLicense,
            bound.SyndicateCard,
            publicApiBaseUrl,
            cancellationToken);

        if (saveResult.IsFailure)
        {
            return saveResult.ToProblem();
        }

        SavedDoctorDocuments saved = saveResult.Value;

        var command = new RegisterUserCommand
        {
            FullName = bound.FullName,
            Email = bound.Email,
            Password = bound.Password,
            Gender = bound.Gender,
            Age = bound.Age,
            Role = bound.Role,
            PhoneNumber = bound.PhoneNumber,
            YearsOfExperience = bound.YearsOfExperience,
            ClinicName = bound.ClinicName,
            ProfessionalBio = bound.ProfessionalBio,
            PracticeLicenseUrl = saved.PracticeLicenseUrl,
            SyndicateCardUrl = saved.SyndicateCardUrl
        };

        Result<RegisterResponseDto> result =
            await mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            documentStorage.TryDeleteFolder(saved.FolderPath);
            return result.ToProblem();
        }

        return Results.Ok(result);
    }

    private static bool IsJson(string? contentType) =>
        contentType is not null
        && contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);

    private static bool IsMultipart(string? contentType) =>
        contentType is not null
        && contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase);
}
