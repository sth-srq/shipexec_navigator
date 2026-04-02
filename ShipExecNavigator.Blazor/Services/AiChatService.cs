using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShipExecNavigator.Shared.AI;
using ShipExecNavigator.Shared.Interfaces;

namespace ShipExecNavigator.Services;

public sealed class AiChatService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<AiChatService> logger) : IAiChatService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<string> SendMessageAsync(IReadOnlyList<ChatMessage> history, string userMessage, string? xmlContext = null, bool useRag = true, CancellationToken ct = default)
    {
        var apiKey  = configuration["AiChat:ApiKey"]  ?? string.Empty;
        var baseUrl = configuration["AiChat:BaseUrl"]  ?? "https://api.openai.com/v1/";
        var model   = configuration["AiChat:Model"]    ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
            return "⚠️ No AI API key configured. Add `AiChat:ApiKey` to appsettings.json.";

        logger.LogTrace(">> SendMessageAsync | Model={Model} History={History}", model, history.Count);
        var sw      = System.Diagnostics.Stopwatch.StartNew();
        var preview = userMessage.Length > 200 ? userMessage[..200] + "..." : userMessage;
        logger.LogInformation(
            "OpenAI request | Model={Model} BaseUrl={BaseUrl} HistoryCount={History} MessagePreview={Preview}",
            model, baseUrl, history.Count, preview);

        var messages = new List<object>();

        if (!string.IsNullOrWhiteSpace(xmlContext))
        {
            messages.Add(new
            {
                role    = "system",
                content = "You are a helpful assistant for ShipExec Navigator. " +
                          "The user has a ShipExec XML configuration loaded. " +
                          "When writing JavaScript to manipulate the Navigator tree view, " +
                          "use only the class names and IDs documented in the DOM reference below." +
                          NavigatorDomCheatSheet.Content
            });
        }

        messages.AddRange(history
            .Select(m => (object)new { role = m.Role, content = m.Content }));
        messages.Add(new { role = "user", content = userMessage });

        var body = JsonSerializer.Serialize(new { model, messages }, _json);

        var client = httpClientFactory.CreateClient("AiChat");
        client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await client.PostAsync(
            "chat/completions",
            new StringContent(body, Encoding.UTF8, "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "OpenAI error response | Model={Model} StatusCode={StatusCode} DurationMs={DurationMs} Body={Body}",
                model, (int)response.StatusCode, sw.ElapsedMilliseconds,
                raw.Length > 500 ? raw[..500] : raw);
            return $"⚠️ API error {(int)response.StatusCode}: {raw}";
        }

        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("message")
                  .GetProperty("content")
                  .GetString() ?? "(empty response)";

        logger.LogInformation(
            "OpenAI response | Model={Model} ResponseLength={Length} DurationMs={DurationMs}",
            model, content.Length, sw.ElapsedMilliseconds);

        return content;
    }
}
