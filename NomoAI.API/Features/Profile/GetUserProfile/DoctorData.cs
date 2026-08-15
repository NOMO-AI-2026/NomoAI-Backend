namespace NomoAI.API.Features.Profile.GetUserProfile
{
    public class DoctorData
    {
        public int? YearsOfExperience { get; set; }
        public string? ClinicName { get; set; }
        public string? ProfessionalBio { get; set; }

        /// <summary>
        /// Verification documents submitted at registration. Returned for the
        /// owning doctor to review; UpdateUserProfile does not change these fields.
        /// </summary>
        public string? IdentityDocumentUrl { get; set; }

        public string? PracticeLicenseUrl { get; set; }

        public string? SyndicateCardUrl { get; set; }

        public string? SyndicateRegistrationNumber { get; set; }

        public int AvailableMinutes { get ; set; }
    }
}
