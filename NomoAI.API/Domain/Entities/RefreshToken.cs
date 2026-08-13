namespace NomoAI.API.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public string? ReplacedByTokenHash { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}
