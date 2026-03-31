using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShipExecNavigator.Shared.Interfaces;

namespace ShipExecNavigator.Services;

public sealed class AiChatService(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IAiChatService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<string> SendMessageAsync(IReadOnlyList<ChatMessage> history, string userMessage, string? xmlContext = null, bool useRag = true, CancellationToken ct = default)
    {
        var apiKey  = configuration["AiChat:ApiKey"]  ?? string.Empty;
        var baseUrl = configuration["AiChat:BaseUrl"]  ?? "https://api.openai.com/v1/";
        var model   = configuration["AiChat:Model"]    ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
            return "⚠️ No AI API key configured. Add `AiChat:ApiKey` to appsettings.json.";

        var messages = history
            .Select(m => new { role = m.Role, content = m.Content })
            .Concat([new { role = "user", content = userMessage }])
            .ToList();

        var body = JsonSerializer.Serialize(new { model, messages }, _json);

        var client = httpClientFactory.CreateClient("AiChat");
        client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await client.PostAsync(
            "chat/completions",
            new StringContent(body, Encoding.UTF8, "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return $"⚠️ API error {(int)response.StatusCode}: {raw}";

        using var doc = JsonDocument.Parse(raw);
        return doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("message")
                  .GetProperty("content")
                  .GetString() ?? "(empty response)";
    }
}
