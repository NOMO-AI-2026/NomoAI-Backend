namespace NomoAI.API.Features.AdminDashboard.GetAllDoctors
{
    public class DoctorResponse
    {
        public string UserId { get; set; }

        public string FullName { get; set; } 

        public string Email { get; set; }
        
        public bool IsApproved { get; set; }

        public int? YearsOfExperience { get; set; }

        public string? ClinicName { get; set; }

        public string? ProfessionalBio { get; set; }

        public string? IdentityDocumentUrl { get; set; }

        public string? PracticeLicenseUrl { get; set; }

        public string? SyndicateCardUrl { get; set; }

        public string? SyndicateRegistrationNumber { get; set; }
    }
}
