namespace NomoAI.API.Domain.Entities
{
    public class SessionAttempts : BaseEntity<int>
    {
        public int SessionId { get; set; }

        public int AttemptNumber { get; set; }

        /// <summary>
        /// Stable API path/URL for the child's attempt audio (not a temp upload path).
        /// </summary>
        public string? AudioUrl { get; set; }

        /// <summary>Raw child attempt audio bytes persisted for doctor review.</summary>
        public byte[]? AudioContent { get; set; }

        /// <summary>MIME type of <see cref="AudioContent"/> (e.g. audio/webm).</summary>
        public string? AudioContentType { get; set; }

        public Session Session { get; set; } = null!;
    }
}
