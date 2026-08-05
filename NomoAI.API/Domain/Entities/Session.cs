using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Domain.Entities
{
    public class Session : BaseEntity<int>
    {
        public int ChildId { get; set; }

        public int ActivityId { get; set; }

        public string SessionTitle { get; set; }

        public SessionStatus Status { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public string? ParentNotes { get; set; }

        /// <summary>
        /// False until the doctor explicitly reviews a completed session.
        /// Used by doctor dashboard "awaiting doctor review" counters.
        /// </summary>
        public bool IsDoctorReviewed { get; set; } = false;

        /// <summary>
        /// Doctor star rating (1–5). Null until the doctor reviews the session.
        /// </summary>
        public int? DoctorRating { get; set; }

        /// <summary>
        /// Doctor review comment. Null until the doctor reviews the session.
        /// </summary>
        public string? DoctorComment { get; set; }

        public Children Child { get; set; } = null!;

        public Activity Activity { get; set; } = null!;
    }
}
