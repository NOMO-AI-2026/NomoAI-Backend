using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Features.AdminDashboard.GetDoctorDetails
{
    public sealed class DoctorDetailsResponse
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public int Age { get; set; }

        public Gender Gender { get; set; }

        public int? YearsOfExperience { get; set; }

        public string? ClinicName { get; set; }

        public string? ProfessionalBio { get; set; }

        public bool IsApproved { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
