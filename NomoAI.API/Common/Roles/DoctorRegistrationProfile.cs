namespace NomoAI.API.Common.Roles;

public sealed record DoctorRegistrationProfile(
    int? YearsOfExperience = null,
    string? ClinicName = null,
    string? ProfessionalBio = null,
    string? PracticeLicenseUrl = null,
    string? SyndicateCardUrl = null);
