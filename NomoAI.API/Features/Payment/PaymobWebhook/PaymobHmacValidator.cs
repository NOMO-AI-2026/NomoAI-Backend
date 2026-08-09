using System.Security.Cryptography;
using System.Text;

namespace NomoAI.API.Features.Payment.PaymobWebhook
{
    public static class PaymobHmacValidator
    {
        public static bool IsValid(PaymobTransactionObjDto obj, string receivedHmac, string hmacKey)
        {
            if (obj is null || string.IsNullOrWhiteSpace(receivedHmac) || string.IsNullOrWhiteSpace(hmacKey))
            {
                return false;
            }

            var concatenated = string.Concat(
                obj.AmountCents.ToString(),
                obj.CreatedAt ?? string.Empty,
                obj.Currency ?? string.Empty,
                ToPaymobBool(obj.ErrorOccured),
                ToPaymobBool(obj.HasParentTransaction),
                obj.Id.ToString(),
                obj.IntegrationId.ToString(),
                ToPaymobBool(obj.Is3dSecure),
                ToPaymobBool(obj.IsAuth),
                ToPaymobBool(obj.IsCapture),
                ToPaymobBool(obj.IsRefunded),
                ToPaymobBool(obj.IsStandalonePayment),
                ToPaymobBool(obj.IsVoided),
                (obj.Order?.Id ?? 0).ToString(),
                obj.Owner.ToString(),
                ToPaymobBool(obj.Pending),
                obj.SourceData?.Pan ?? string.Empty,
                obj.SourceData?.SubType ?? string.Empty,
                obj.SourceData?.Type ?? string.Empty,
                ToPaymobBool(obj.Success));

            var keyBytes = Encoding.UTF8.GetBytes(hmacKey);
            var dataBytes = Encoding.UTF8.GetBytes(concatenated);

            using var hmac = new HMACSHA512(keyBytes);
            var hash = hmac.ComputeHash(dataBytes);
            var computed = Convert.ToHexString(hash).ToLowerInvariant();
            var received = receivedHmac.Trim().ToLowerInvariant();

            var computedBytes = Encoding.UTF8.GetBytes(computed);
            var receivedBytes = Encoding.UTF8.GetBytes(received);

            if (computedBytes.Length != receivedBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(computedBytes, receivedBytes);
        }

        private static string ToPaymobBool(bool value) => value ? "true" : "false";
    }
}
