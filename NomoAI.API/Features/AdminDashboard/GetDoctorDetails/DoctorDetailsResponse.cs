using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails;

public sealed class DoctorDetailsResponse
{
    public int Id { get; init; }

    public required string UserId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public string? PhoneNumber { get; init; }

    public Gender Gender { get; init; }

    public int Age { get; init; }

    public bool IsApproved { get; init; }

    public int? YearsOfExperience { get; init; }

    public string? ClinicName { get; init; }

    public string? ProfessionalBio { get; init; }

    public string? PracticeLicenseUrl { get; init; }

    public string? SyndicateCardUrl { get; init; }

    public DateTime CreatedAt { get; init; }
}
