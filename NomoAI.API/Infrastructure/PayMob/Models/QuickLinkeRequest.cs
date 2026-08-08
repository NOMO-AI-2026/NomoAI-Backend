using System;
using System.Text.Json.Serialization;

namespace NomoAI.API.Infrastructure.PayMob.Models
{
    public class QuickLinkRequest
    {
        [JsonPropertyName("amount_cents")]
        public string AmountCents { get; set; } = string.Empty;

        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("reference_id")]
        public string ReferenceId { get; set; } = string.Empty;

        [JsonPropertyName("payment_methods")]
        public string[] PaymentMethods { get; set; } = Array.Empty<string>();

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("is_live")]
        public bool IsLive { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
