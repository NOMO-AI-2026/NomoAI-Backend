using NomoAI.API.Domain.Enums;

namespace NomoAI.API.Domain.Entities
{
    public class SupportTicket : BaseEntity<int>
    {
        public string UserId { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;

        public string? AdminNote { get; set; }

        public string? HandledByAdminUserId { get; set; }

        public DateTime? HandledAt { get; set; }

        public ApplicationUser User { get; set; } = null!;
    }
}
