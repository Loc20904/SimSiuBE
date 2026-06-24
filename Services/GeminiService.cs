using System.Text;
using System.Text.Json;

namespace ViettalAPI.Services
{
    public interface IGeminiService
    {
        Task<string> ChatAsync(string systemInstruction, string userMessage);
        Task<string> ListModelsAsync();
    }

    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"] ?? string.Empty;
            _model = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        }

        public async Task<string> ListModelsAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "{\"error\": \"API Key is empty.\"}";
            }

            try
            {
                var response = await _httpClient.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}");
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
        }

        public async Task<string> ChatAsync(string systemInstruction, string userMessage)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "⚠️ Chào bạn! Hệ thống AI hiện chưa được cấu hình API Key. Vui lòng mở file `appsettings.json` ở Backend và cập nhật trường `Gemini:ApiKey` với khóa API hợp lệ của bạn để có thể trò chuyện với Trợ lý AI nhé.";
            }

            try
            {
                var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                // Build request payload for Gemini API
                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[]
                            {
                                new { text = userMessage }
                            }
                        }
                    },
                    system_instruction = new
                    {
                        parts = new[]
                        {
                            new { text = systemInstruction }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(requestUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    return $"⚠️ Lỗi kết nối API Gemini (Status: {response.StatusCode}): {errorMsg}";
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                
                // Navigate Gemini response structure: candidates[0].content.parts[0].text
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && 
                    candidates.ValueKind == JsonValueKind.Array && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentProp) &&
                        contentProp.TryGetProperty("parts", out var parts) &&
                        parts.ValueKind == JsonValueKind.Array &&
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        return text ?? "⚠️ Trợ lý AI trả về phản hồi rỗng.";
                    }
                }

                return "⚠️ Không thể phân tích phản hồi từ Gemini API.";
            }
            catch (Exception ex)
            {
                return $"⚠️ Đã xảy ra lỗi khi gọi Trợ lý AI: {ex.Message}";
            }
        }
    }
}
