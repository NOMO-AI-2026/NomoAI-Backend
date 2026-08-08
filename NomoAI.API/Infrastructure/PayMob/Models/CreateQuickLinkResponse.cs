using System;
using System.Text.Json.Serialization;

namespace NomoAI.API.Infrastructure.PayMob.Models
{
    public class CreateQuickLinkResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("client_info")]
        public ClientInfoDto? ClientInfo { get; set; }

        [JsonPropertyName("reference_id")]
        public string? ReferenceId { get; set; }

        [JsonPropertyName("shorten_url")]
        public string? ShortenUrl { get; set; }

        [JsonPropertyName("amount_cents")]
        public int AmountCents { get; set; }

        [JsonPropertyName("payment_link_image")]
        public string? PaymentLinkImage { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [JsonPropertyName("client_url")]
        public string? ClientUrl { get; set; }

        [JsonPropertyName("origin")]
        public int? Origin { get; set; }

        [JsonPropertyName("merchant_staff_tag")]
        public string? MerchantStaffTag { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("paid_at")]
        public DateTime? PaidAt { get; set; }

        [JsonPropertyName("redirection_url")]
        public string? RedirectionUrl { get; set; }

        [JsonPropertyName("notification_url")]
        public string? NotificationUrl { get; set; }

        [JsonPropertyName("order")]
        public long? Order { get; set; }
    }

    public class ClientInfoDto
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }
    }
}
