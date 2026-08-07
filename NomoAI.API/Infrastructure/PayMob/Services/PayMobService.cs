using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NomoAI.API.Infrastructure.PayMob.Models;

namespace NomoAI.API.Infrastructure.PayMob.Services
{
    public class PayMobService(IConfiguration config) : IPayMobService
    {
        private readonly IConfiguration _config = config;

        public async Task<CreateQuickLinkResponse> CreateQuickLinkAsync(QuickLinkRequest request)
        {
            string authToken = await GetAuthTokenAsync();

            var baseUrl = _config["Paymob:BaseUrl"] ?? _config["PayMob:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Paymob base url is not configured.");

            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var payload = new
            {
                amount_cents = request.AmountCents,
                expires_at = request.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                reference_id = request.ReferenceId,
                payment_methods = request.PaymentMethods,
                email = request.Email,
                is_live = request.IsLive,
                full_name = request.FullName,
                phone_number = request.PhoneNumber,
                description = request.Description
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/ecommerce/payment-links", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonSerializer.Deserialize<CreateQuickLinkResponse>(responseString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result is null)
                    throw new InvalidOperationException("Failed to deserialize Paymob quick link response.");

                return result;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse Paymob quick link response.", ex);
            }
        }

        public async Task<string> GetAuthTokenAsync()
        {
            var baseUrl = _config["Paymob:BaseUrl"] ?? _config["PayMob:BaseUrl"];
            var apiKey = _config["Paymob:ApiKey"] ?? _config["PayMob:ApiKey"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Paymob base url is not configured.");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Paymob api key is not configured.");

            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

            var payload = new { api_key = apiKey };
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/api/auth/tokens", content);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();

            try
            {
                var auth = JsonSerializer.Deserialize<CreateAuthTokenResponse>(responseString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (auth is null)
                    throw new InvalidOperationException("Failed to deserialize Paymob auth response.");

                return auth.Token ?? string.Empty;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse Paymob response.", ex);
            }
        }
    }
}
