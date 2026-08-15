namespace NomoAI.API.Features.Profile.GetUserProfile
{
    public class DoctorData
    {
        public int? YearsOfExperience { get; set; }
        public string? ClinicName { get; set; }
        public string? ProfessionalBio { get; set; }

        /// <summary>
        /// Active verification documents. Replacement files submitted via
        /// PUT /api/profile/doctor-documents appear as pending until an admin accepts them.
        /// UpdateUserProfile does not change these fields.
        /// </summary>
        public string? PracticeLicenseUrl { get; set; }

        public string? SyndicateCardUrl { get; set; }

        public string? PendingPracticeLicenseUrl { get; set; }

        public string? PendingSyndicateCardUrl { get; set; }

        public bool HasPendingDocuments { get; set; }

        public int AvailableMinutes { get ; set; }
    }
}
