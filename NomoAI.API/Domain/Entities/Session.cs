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

        /// <summary>Full AiSessionPlanV2Response snapshot captured at StartSession time.</summary>
        public string? PlanJson { get; set; }

        public int CurrentStepNumber { get; set; } = 1;

        public int CurrentAttemptNumber { get; set; }

        public string? PlanModel { get; set; }

        public bool PlanUsedFallback { get; set; }

        public DateTimeOffset? PlanGeneratedAt { get; set; }

        public string? KnowledgeSourceIdsJson { get; set; }

        public string? KnowledgeChunkIdsJson { get; set; }

        public bool RequiresDoctorReview { get; set; }

        /// <summary>Denormalized AI activity type, e.g. "word".</summary>
        public string? ActivityType { get; set; }

        /// <summary>Denormalized therapy target/prompt sent to AI Core.</summary>
        public string? Prompt { get; set; }

        public string? SpeechLevel { get; set; }

        public string Language { get; set; } = "ar";

        public int? ChildAge { get; set; }
    }
}
