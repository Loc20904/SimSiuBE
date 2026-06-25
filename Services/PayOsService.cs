using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ViettalAPI.Services
{
    public interface IPayOsService
    {
        Task<PayOsCreatePaymentResult> CreatePaymentLinkAsync(PayOsCreatePaymentRequest request);
        bool IsValidWebhookSignature<TData>(TData data, string signature);
    }

    public class PayOsService : IPayOsService
    {
        private readonly HttpClient _httpClient;
        private readonly PayOsOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        public PayOsService(HttpClient httpClient, IOptions<PayOsOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<PayOsCreatePaymentResult> CreatePaymentLinkAsync(PayOsCreatePaymentRequest request)
        {
            EnsureConfigured();

            request.Signature = CreateSignature(new SortedDictionary<string, object?>
            {
                ["amount"] = request.Amount,
                ["cancelUrl"] = request.CancelUrl,
                ["description"] = request.Description,
                ["orderCode"] = request.OrderCode,
                ["returnUrl"] = request.ReturnUrl
            });

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v2/payment-requests");
            httpRequest.Headers.Add("x-client-id", _options.ClientId);
            httpRequest.Headers.Add("x-api-key", _options.ApiKey);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await _httpClient.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"payOS returned HTTP {(int)response.StatusCode}: {body}");
            }

            var payOsResponse = JsonSerializer.Deserialize<PayOsCreatePaymentResponse>(body, _jsonOptions)
                ?? throw new InvalidOperationException("payOS response is empty.");

            if (payOsResponse.Code != "00" || payOsResponse.Data == null)
            {
                throw new InvalidOperationException($"payOS rejected payment link request: {payOsResponse.Desc}");
            }

            return payOsResponse.Data;
        }

        public bool IsValidWebhookSignature<TData>(TData data, string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            var dataJson = JsonSerializer.Serialize(data, _jsonOptions);
            using var document = JsonDocument.Parse(dataJson);
            var values = new SortedDictionary<string, object?>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = ToSignatureValue(property.Value);
            }

            var computed = CreateSignature(values);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(signature));
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.ClientId) ||
                string.IsNullOrWhiteSpace(_options.ApiKey) ||
                string.IsNullOrWhiteSpace(_options.ChecksumKey))
            {
                throw new InvalidOperationException("PayOS is not configured. Set PayOS:ClientId, PayOS:ApiKey, and PayOS:ChecksumKey.");
            }
        }

        private string CreateSignature(SortedDictionary<string, object?> values)
        {
            var data = string.Join("&", values.Select(item => $"{item.Key}={FormatSignatureValue(item.Value)}"));
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.ChecksumKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static object? ToSignatureValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => string.Empty,
                JsonValueKind.Undefined => string.Empty,
                JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
                JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(arrayItem => arrayItem.ValueKind == JsonValueKind.Object
                        ? arrayItem.EnumerateObject()
                            .OrderBy(prop => prop.Name, StringComparer.Ordinal)
                            .ToDictionary(prop => prop.Name, prop => ToSignatureValue(prop.Value))
                        : ToSignatureValue(arrayItem))
                    .ToList(),
                _ => element.GetString() ?? string.Empty
            };
        }

        private static string FormatSignatureValue(object? value)
        {
            return value switch
            {
                null => string.Empty,
                bool boolValue => boolValue ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };
        }
    }

    public class PayOsCreatePaymentRequest
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
        public List<PayOsPaymentItem> Items { get; set; } = new();
        public string CancelUrl { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public long ExpiredAt { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public class PayOsPaymentItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Price { get; set; }
    }

    public class PayOsCreatePaymentResponse
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public PayOsCreatePaymentResult? Data { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public class PayOsCreatePaymentResult
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string PaymentLinkId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
    }
}
