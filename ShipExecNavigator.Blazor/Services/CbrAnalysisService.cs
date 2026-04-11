using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Services;

/// <summary>
/// Scoped service that rewrites a CBR script using CBRHelper methods and
/// appends a self-contained <c>CBRHelperUsed</c> object at the bottom
/// containing only the CBRHelper methods that were actually referenced.
///
/// <para>
/// <b>Two-pass approach:</b>
/// <list type="number">
///   <item>
///     Pass 1 — Send the CBR script + full CBRHelper source to Azure OpenAI.
///     Returns JSON: <c>{ "rewritten": "...", "usedMethods": ["CBRHelper.X.y", ...] }</c>.
///   </item>
///   <item>
///     Pass 2 — Send the list of used method names + CBRHelper source back to
///     the AI.  It extracts those methods' full implementations (with original
///     JSDoc comments) into a <c>var CBRHelperUsed = { ... };</c> block.
///   </item>
/// </list>
/// The final output is the rewritten CBR followed by a blank line and the
/// extracted <c>CBRHelperUsed</c> block.
/// </para>
/// </summary>
public sealed class CbrAnalysisService(
    IConfiguration configuration,
    IWebHostEnvironment env,
    IHttpClientFactory httpClientFactory,
    ILogger<CbrAnalysisService> logger) : ICbrAnalysisService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    // ── Pass 1: rewrite CBR + identify used CBRHelper methods ─────────────
    private const string _pass1SystemPrompt =
        """
        You are an expert JavaScript engineer specializing in ShipExec Client Business Rules (CBR).

        A CBR is a JavaScript constructor function containing hook methods such as PageLoaded,
        PreShip, PostShip, PostRate, PreBuildShipment, PostBuildShipment, NewShipment, etc.
        The CBRHelper library exposes a rich set of utility methods for common CBR tasks
        (field access, carrier/service filtering, rate shop, address book helpers, etc.).

        ## Your task
        Rewrite the provided CBR script so that:
        1. Every place where a CBRHelper method covers the same logic, replace the inline code
           with the CBRHelper call.  Add a comment directly above the change, e.g.:
               // [CBRHelper] Replaced manual field read with CBRHelper.getField(...)
        2. Fix all variable naming to strict camelCase (e.g. MyVar -> myVar, _MyVar -> myVar).
           Add a comment on the FIRST renamed occurrence of each variable:
               // [Renamed] MyVar -> myVar
        3. Normalise brace style to K&R (opening brace on same line as the statement).
           Add ONE block comment at the top of the rewritten function if braces were changed:
               // [Style] Brace style normalised to K&R
        4. Do NOT change business logic, hook method names, or overall structure.
        5. After rewriting, list every CBRHelper method you used in the rewritten script
           using the full dotted path (e.g. "CBRHelper.Utilities.dateTimeStamp").

        ## Required response format
        Respond with EXACTLY ONE valid JSON object — no markdown, no code fences.
        Use this structure:
        {
          "rewritten": "<the complete rewritten CBR JavaScript as a plain string>",
          "usedMethods": ["CBRHelper.Section.method1", "CBRHelper.Section.method2"]
        }

        The "rewritten" value must be the full script, not a summary.
        Escape all double-quotes inside the script with \".
        The "usedMethods" array must contain only methods that appear in the rewritten script.
        If no CBRHelper methods were used, return an empty array.
        """;

    // ── Pass 2: extract full implementations of used methods ──────────────
    private const string _pass2SystemPrompt =
        """
        You are an expert JavaScript engineer.

        You will be given:
        1. A list of CBRHelper method names that were used in a rewritten CBR script.
        2. The full CBRHelper.js source.

        Your task:
        Build a JavaScript object called CBRHelperUsed that contains ONLY the sections
        and methods from that list, taken directly from CBRHelper.js.

        Rules:
        - Preserve each method's original implementation exactly — do not paraphrase or trim.
        - Preserve the original JSDoc/inline comments above each method.
        - Group methods under the same sub-object names they have in CBRHelper
          (e.g. CBRHelperUsed.Utilities, CBRHelperUsed.Logger, etc.).
        - Only include sections that have at least one used method.
        - Wrap the whole thing as shown below. The separator comment and object name must
          match exactly:

              // ── CBRHelper methods used by this script ──────────────────────
              var CBRHelperUsed = {
                  SectionName: {
                      // <original comment>
                      methodName: function (...) { ... },
                      ...
                  },
                  ...
              };

        - The very first line of your response must be exactly:
              // ── CBRHelper methods used by this script ──────────────────────
        - Output ONLY the raw JavaScript block — no markdown, no code fences, no explanation.
        """;

    public async Task<string> AnalyzeCbrAsync(string cbrScript, CancellationToken ct = default)
    {
        var endpoint   = configuration["AzureOpenAI:Endpoint"]      ?? string.Empty;
        var apiKey     = configuration["AzureOpenAI:ApiKey"]         ?? string.Empty;
        var deployment = configuration["AzureOpenAI:ChatDeployment"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
            return "// ⚠️ Azure OpenAI is not configured. Set AzureOpenAI:ApiKey in appsettings.json.";

        var helperPath = Path.Combine(env.WebRootPath, "downloads", "ShipExecNavigator_CBRHelper.js");
        if (!File.Exists(helperPath))
            return $"// ⚠️ CBRHelper.js not found at: {helperPath}";

        var helperContent = await File.ReadAllTextAsync(helperPath, ct);

        // ── Pass 1: rewrite CBR + get list of used methods ─────────────────
        logger.LogInformation(
            "CbrAnalysis Pass1 | Deployment={Deployment} CbrLength={CbrLen} HelperLength={HelperLen}",
            deployment, cbrScript.Length, helperContent.Length);

        var pass1Message = new StringBuilder();
        pass1Message.AppendLine("=== CURRENT CBR SCRIPT ===");
        pass1Message.AppendLine(cbrScript);
        pass1Message.AppendLine();
        pass1Message.AppendLine("=== CBRHELPER LIBRARY ===");
        pass1Message.AppendLine(helperContent);

        var pass1Raw = await CallOpenAiAsync(
            deployment, endpoint, apiKey,
            _pass1SystemPrompt,
            pass1Message.ToString(),
            responseFormatJson: true,
            ct);

        if (pass1Raw.StartsWith("// ⚠️"))
            return pass1Raw;

        // Parse pass-1 JSON response
        string rewritten;
        List<string> usedMethods;
        try
        {
            using var doc = JsonDocument.Parse(pass1Raw);
            var root = doc.RootElement;
            rewritten = root.GetProperty("rewritten").GetString() ?? cbrScript;
            usedMethods = root.GetProperty("usedMethods")
                .EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                "CbrAnalysis Pass1 JSON parse failed ({Error}) — returning raw AI text.", ex.Message);
            return pass1Raw;
        }

        logger.LogInformation(
            "CbrAnalysis Pass1 complete | RewrittenLength={Len} UsedMethodCount={Count} Methods={Methods}",
            rewritten.Length, usedMethods.Count, string.Join(", ", usedMethods));

        // If no CBRHelper methods were referenced, return just the rewritten script
        if (usedMethods.Count == 0)
            return rewritten;

        // ── Pass 2: extract full source for the used methods ────────────────
        logger.LogInformation(
            "CbrAnalysis Pass2 | Deployment={Deployment} Extracting {Count} method(s)",
            deployment, usedMethods.Count);

        var pass2Message = new StringBuilder();
        pass2Message.AppendLine("=== USED CBRHELPER METHODS ===");
        foreach (var method in usedMethods)
            pass2Message.AppendLine($"  {method}");
        pass2Message.AppendLine();
        pass2Message.AppendLine("=== CBRHELPER.JS SOURCE ===");
        pass2Message.AppendLine(helperContent);

        var extractedBlock = await CallOpenAiAsync(
            deployment, endpoint, apiKey,
            _pass2SystemPrompt,
            pass2Message.ToString(),
            responseFormatJson: false,
            ct);

        if (extractedBlock.StartsWith("// ⚠️"))
        {
            logger.LogWarning("CbrAnalysis Pass2 failed — returning rewritten script without appendix.");
            return rewritten;
        }

        return rewritten + "\n\n" + extractedBlock;
    }

    // ── Shared Azure OpenAI call (single-turn) ──────────────────────────────
    private async Task<string> CallOpenAiAsync(
        string deployment,
        string endpoint,
        string apiKey,
        string systemPrompt,
        string userMessage,
        bool responseFormatJson,
        CancellationToken ct)
    {
        var messages = new object[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user",   content = userMessage  }
        };

        object body = responseFormatJson
            ? (object)new { model = deployment, messages, response_format = new { type = "json_object" } }
            : new { model = deployment, messages };

        var bodyJson = JsonSerializer.Serialize(body, _json);

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/");
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        client.Timeout = TimeSpan.FromMinutes(5);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        using var response = await client.PostAsync(
            "chat/completions?api-version=2025-01-01-preview",
            new StringContent(bodyJson, Encoding.UTF8, "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "CbrAnalysis API error | StatusCode={StatusCode} DurationMs={DurationMs} Body={Body}",
                (int)response.StatusCode, sw.ElapsedMilliseconds,
                raw.Length > 500 ? raw[..500] : raw);
            return $"// ⚠️ API error {(int)response.StatusCode} — check server logs for details.";
        }

        using var doc = JsonDocument.Parse(raw);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        logger.LogInformation(
            "CbrAnalysis API response | Deployment={Deployment} ContentLength={Length} DurationMs={DurationMs}",
            deployment, content.Length, sw.ElapsedMilliseconds);

        return content;
    }

    // ── Conversational CBR chat ──────────────────────────────────────────────
    private const string _chatSystemBase =
        """
        You are an expert JavaScript engineer specializing in ShipExec Client Business Rules (CBR).

        A CBR is a JavaScript constructor function containing hook methods such as PageLoaded,
        PreShip, PostShip, PostRate, PreBuildShipment, PostBuildShipment, NewShipment, etc.
        The CBRHelper library provides utility methods for field access, carrier/service filtering,
        rate shop, address book helpers, logging, UI, and more.

        The user is currently working on the following CBR script:
        """;

    private const string _chatSystemTail =
        """

        Answer questions, explain code, suggest improvements, and help debug issues.
        When providing a full revised version of the script, place the complete rewritten script
        in the "code" field. Only set "code" when you are providing a COMPLETE updated script —
        never for partial snippets or code fragments.

        Respond with EXACTLY ONE valid JSON object — no markdown, no code fences outside the JSON.
        Use exactly this structure:
        {
          "message": "<your conversational reply>",
          "code": "<complete revised CBR script — omit this key entirely when not providing a full rewrite>"
        }
        """;

    public async Task<CbrChatResponse> ChatCbrAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        string cbrScript,
        CancellationToken ct = default)
    {
        var endpoint   = configuration["AzureOpenAI:Endpoint"]      ?? string.Empty;
        var apiKey     = configuration["AzureOpenAI:ApiKey"]         ?? string.Empty;
        var deployment = configuration["AzureOpenAI:ChatDeployment"] ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
            return new CbrChatResponse
            {
                Message = "⚠️ Azure OpenAI is not configured. Set AzureOpenAI:ApiKey in appsettings.json."
            };

        var systemPrompt = _chatSystemBase
            + "\n=== CURRENT CBR SCRIPT ===\n"
            + cbrScript
            + "\n=== END CBR SCRIPT ==="
            + _chatSystemTail;

        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        foreach (var h in history)
            messages.Add(new { role = h.Role, content = h.Content });
        messages.Add(new { role = "user", content = userMessage });

        logger.LogInformation(
            "CbrChat | Deployment={Deployment} HistoryCount={Count} UserMsgLen={Len}",
            deployment, history.Count, userMessage.Length);

        var raw = await CallOpenAiMessagesAsync(deployment, endpoint, apiKey, messages, ct);

        if (raw.StartsWith("// ⚠️"))
            return new CbrChatResponse { Message = raw };

        try
        {
            using var doc  = JsonDocument.Parse(raw);
            var root       = doc.RootElement;
            var message    = root.TryGetProperty("message", out var mp) ? mp.GetString() ?? raw : raw;
            var code       = root.TryGetProperty("code",    out var cp) ? cp.GetString()        : null;
            if (string.IsNullOrWhiteSpace(code)) code = null;
            return new CbrChatResponse { Message = message, Code = code };
        }
        catch (JsonException)
        {
            return new CbrChatResponse { Message = raw };
        }
    }

    // ── Shared Azure OpenAI call (multi-turn) ────────────────────────────────
    private async Task<string> CallOpenAiMessagesAsync(
        string deployment,
        string endpoint,
        string apiKey,
        IReadOnlyList<object> messages,
        CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new
        {
            model    = deployment,
            messages,
            response_format = new { type = "json_object" }
        }, _json);

        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri($"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/");
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        client.Timeout = TimeSpan.FromMinutes(5);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var response = await client.PostAsync(
            "chat/completions?api-version=2025-01-01-preview",
            new StringContent(body, Encoding.UTF8, "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "CbrChat API error | StatusCode={StatusCode} DurationMs={DurationMs} Body={Body}",
                (int)response.StatusCode, sw.ElapsedMilliseconds,
                raw.Length > 500 ? raw[..500] : raw);
            return $"// ⚠️ API error {(int)response.StatusCode} — check server logs for details.";
        }

        using var doc = JsonDocument.Parse(raw);
        var content   = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        logger.LogInformation(
            "CbrChat API response | Deployment={Deployment} ContentLength={Length} DurationMs={DurationMs}",
            deployment, content.Length, sw.ElapsedMilliseconds);

        return content;
    }
}
