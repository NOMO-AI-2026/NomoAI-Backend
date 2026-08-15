using FluentValidation.Results;
using NomoAI.API.Common.DoctorDocuments;
using NomoAI.API.Common.Enums;
using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.Auth.Register_User;

internal sealed class DoctorRegisterForm
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Password { get; init; }
    public int Age { get; init; }
    public Gender Gender { get; init; }
    public UserRole Role { get; init; }
    public int? YearsOfExperience { get; init; }
    public string? ClinicName { get; init; }
    public string? ProfessionalBio { get; init; }
    public required string SyndicateRegistrationNumber { get; init; }
    public required DoctorDocumentFile IdentityDocument { get; init; }
    public required DoctorDocumentFile PracticeLicense { get; init; }
    public required DoctorDocumentFile SyndicateCard { get; init; }
}

internal static class DoctorRegisterFormBinder
{
    public static bool TryBind(
        IFormCollection form,
        out DoctorRegisterForm? bound,
        out IReadOnlyList<ValidationFailure> failures)
    {
        var errors = new List<ValidationFailure>();

        string fullName = Get(form, "FullName");
        string email = Get(form, "Email");
        string password = Get(form, "Password");
        string syndicateNumber = Get(form, "syndicateRegistrationNumber");
        if (string.IsNullOrWhiteSpace(syndicateNumber))
        {
            syndicateNumber = Get(form, "SyndicateRegistrationNumber");
        }

        Require(errors, "FullName", fullName, "FullName is required.");
        Require(errors, "Email", email, "Email is required.");
        Require(errors, "Password", password, "Password is required.");

        if (!TryParseEnum(Get(form, "Gender"), out Gender gender))
        {
            errors.Add(new ValidationFailure("Gender", "Gender is required. Use 0 for Male or 1 for Female."));
        }

        if (!TryParseEnum(Get(form, "Role"), out UserRole role))
        {
            errors.Add(new ValidationFailure("Role", "Role is required. Use 0 for Doctor."));
        }

        string ageRaw = Get(form, "Age");
        int age = 0;
        if (!string.IsNullOrWhiteSpace(ageRaw) && !int.TryParse(ageRaw, out age))
        {
            errors.Add(new ValidationFailure("Age", "Age must be a number."));
        }

        int? yearsOfExperience = null;
        string yearsRaw = Get(form, "YearsOfExperience");
        if (!string.IsNullOrWhiteSpace(yearsRaw))
        {
            if (int.TryParse(yearsRaw, out int parsedYears))
            {
                yearsOfExperience = parsedYears;
            }
            else
            {
                errors.Add(new ValidationFailure("YearsOfExperience", "Years of experience must be a number."));
            }
        }

        DoctorDocumentFile? identity = ToFile(form.Files.GetFile("identityDocument"));
        DoctorDocumentFile? license = ToFile(form.Files.GetFile("practiceLicense"));
        DoctorDocumentFile? syndicate = ToFile(form.Files.GetFile("syndicateCard"));

        AddFileError(errors, "identityDocument", DoctorDocumentFileRules.Validate(identity, "Identity document"));
        AddFileError(errors, "practiceLicense", DoctorDocumentFileRules.Validate(license, "Practice license"));
        AddFileError(errors, "syndicateCard", DoctorDocumentFileRules.Validate(syndicate, "Syndicate card"));

        if (string.IsNullOrWhiteSpace(syndicateNumber))
        {
            errors.Add(new ValidationFailure(
                "syndicateRegistrationNumber",
                "Syndicate registration number is required for doctor registration."));
        }
        else if (syndicateNumber.Trim().Length > DoctorDocumentLimits.MaxSyndicateRegistrationNumberLength)
        {
            errors.Add(new ValidationFailure(
                "syndicateRegistrationNumber",
                $"Syndicate registration number cannot exceed {DoctorDocumentLimits.MaxSyndicateRegistrationNumberLength} characters."));
        }

        if (errors.Count > 0)
        {
            bound = null;
            failures = errors;
            return false;
        }

        bound = new DoctorRegisterForm
        {
            FullName = fullName,
            Email = email,
            PhoneNumber = EmptyToNull(Get(form, "PhoneNumber")),
            Password = password,
            Age = age,
            Gender = gender,
            Role = role,
            YearsOfExperience = yearsOfExperience,
            ClinicName = EmptyToNull(Get(form, "ClinicName")),
            ProfessionalBio = EmptyToNull(Get(form, "ProfessionalBio")),
            SyndicateRegistrationNumber = syndicateNumber.Trim(),
            IdentityDocument = identity!,
            PracticeLicense = license!,
            SyndicateCard = syndicate!
        };
        failures = Array.Empty<ValidationFailure>();
        return true;
    }

    private static void Require(
        List<ValidationFailure> errors,
        string property,
        string value,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationFailure(property, message));
        }
    }

    private static void AddFileError(
        List<ValidationFailure> errors,
        string property,
        string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            errors.Add(new ValidationFailure(property, message));
        }
    }

    private static string Get(IFormCollection form, string key)
    {
        return form.TryGetValue(key, out var value)
            ? value.ToString()
            : string.Empty;
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

    private static bool TryParseEnum<TEnum>(string raw, out TEnum value)
        where TEnum : struct, Enum
    {
        if (int.TryParse(raw, out int numeric)
            && Enum.IsDefined(typeof(TEnum), numeric))
        {
            value = (TEnum)Enum.ToObject(typeof(TEnum), numeric);
            return true;
        }

        return Enum.TryParse(raw, ignoreCase: true, out value)
            && Enum.IsDefined(typeof(TEnum), value);
    }
}
