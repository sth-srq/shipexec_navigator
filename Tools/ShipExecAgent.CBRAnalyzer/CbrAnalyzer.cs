using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace ShipExecAgent.CBRAnalyzer;

public sealed class CbrAnalyzer
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private static readonly string[] _knownHooks =
    [
        "PageLoaded", "Keystroke",
        "PreShip", "PostShip",
        "PreProcessBatch", "PostProcessBatch",
        "PreVoid", "PostVoid",
        "PrePrint", "PostPrint",
        "PreLoad", "PostLoad",
        "PreRate", "PostRate",
        "PreCloseManifest", "PostCloseManifest",
        "PreTransmit", "PostTransmit",
        "PreSearchHistory", "PostSearchHistory",
        "NewShipment", "PreBuildShipment", "PostBuildShipment", "RepeatShipment",
        "PreCreateGroup", "PostCreateGroup",
        "PreModifyGroup", "PostModifyGroup",
        "PreCloseGroup", "PostCloseGroup",
        "AddPackage", "CopyPackage", "RemovePackage",
        "PreviousPackage", "NextPackage",
        "PostSelectAddressBook",
        "PreShipOrder", "PostShipOrder"
    ];

    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly string _helperOutputPath;
    private readonly bool _isAzure;

    public CbrAnalyzer(IConfiguration configuration)
    {
        _apiKey           = configuration["OpenAI:ApiKey"]          ?? string.Empty;
        _baseUrl          = configuration["OpenAI:BaseUrl"]          ?? "https://api.openai.com/v1/";
        _model            = configuration["OpenAI:Model"]            ?? "gpt-4o-mini";
        _helperOutputPath = @"C:\ShipExecCBR\Analysis\";

        // Azure OpenAI base URLs contain .openai.azure.com
        _isAzure = _baseUrl.Contains(".openai.azure.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Analyzes a single CBR JS file against the current helper file.
    /// Updates the helper file with any newly discovered reusable patterns,
    /// saves it with a unique timestamped name, and returns the new path.
    /// </summary>
    /// <param name="filePath">Path to the CBR JS file to analyze.</param>
    /// <param name="templateContent">Content of the CBR template (hook signatures).</param>
    /// <param name="helperFilePath">
    ///   Path to the current helper JS file.  Pass null/empty for the first file —
    ///   the AI will produce the initial helper from scratch.
    /// </param>
    /// <param name="totalFileCount">Total number of CBR files being processed (for percentage calculation).</param>
    /// <param name="filesAnalyzedSoFar">How many files have been analyzed before this one (0-based).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<CbrPassResult> AnalyzeAsync(
        string filePath,
        string templateContent,
        string? helperFilePath,
        int totalFileCount,
        int filesAnalyzedSoFar,
        CancellationToken ct = default)
    {
        var fileName    = Path.GetFileName(filePath);
        var fileContent = await File.ReadAllTextAsync(filePath, ct);

        var helperContent = string.IsNullOrWhiteSpace(helperFilePath) || !File.Exists(helperFilePath)
            ? string.Empty
            : await File.ReadAllTextAsync(helperFilePath!, ct);

        var totalAnalyzed = filesAnalyzedSoFar + 1;
        var hookList      = string.Join(", ", _knownHooks);

        var systemPrompt =
            $"""
            You are an expert JavaScript analyst for ShipExec ClientBusinessRules (CBR) files.

            A CBR file is a JavaScript constructor function with hook methods ({hookList})
            and a thinClientApiRequest helper for server-side API calls.

            ## Your Task
            Analyze the provided CBR file and identify reusable logic — functions, patterns,
            or techniques that could be extracted into a shared helper JS file used across
            multiple CBR implementations.

            Common reusable patterns to look for:
            - Rate shopping / carrier filtering (PostRate)
            - Field defaulting or validation (PreShip, PostLoad)
            - API calls via thinClientApiRequest
            - Address book logic (PostSelectAddressBook)
            - Shipment field manipulation (PreBuildShipment, PostBuildShipment)
            - Logging or error handling patterns
            - Package-level iteration logic (AddPackage, CopyPackage)

            ## Helper File to Update
            The current helper file is provided in the user message. It contains known topics
            with usage statistics. Your job is to:
            1. Look for existing topics that match patterns in the new CBR file.
               If found: increment the count
            2. If a pattern is genuinely new and reusable, add it as a new topic.
            3. Re-sort all topics so highest-used appears first. Break ties alphabetically.
            4. Output the ENTIRE updated helper file — no explanation, no markdown fences, raw JS only.

            

            ## Required Format for each topic block:

            // <Topic Name>
            // <Number of CBRs that use helpers for this topic>

            // <number of CBRs that use this particular function or pattern>
            <name of proposed function or pattern>

            DO NOT DEFINE THE FUNCTION — just name it as a placeholder for now. The actual implementation will be done by engineers after the analysis.

            Separate topics with a blank line.
            The very first line of the output must be:
            // Topics: <comma-separated topic names sorted by usage desc>

            Add the current filename as a comment on the second line
            // something.js


            DO NOT REMOVE EXISTING METHODS - THIS IS CRUCIAL!!! ONLY ADD AND UPDATE COUNT ARE ALLOWED.

            """;

        var userMessage = new StringBuilder();
        userMessage.AppendLine($"CBR File to analyze: {fileName}");
        userMessage.AppendLine();
        userMessage.AppendLine("=== CBR TEMPLATE (hook signatures for reference) ===");
        userMessage.AppendLine(templateContent);
        userMessage.AppendLine();
        userMessage.AppendLine($"=== CBR FILE: {fileName} ===");
        userMessage.AppendLine(fileContent);
        userMessage.AppendLine();

        if (string.IsNullOrWhiteSpace(helperContent))
        {
            userMessage.AppendLine("=== CURRENT HELPER FILE ===");
            userMessage.AppendLine("(empty — this is the first file; create the helper from scratch)");
        }
        else
        {
            userMessage.AppendLine("=== CURRENT HELPER FILE ===");
            userMessage.AppendLine(helperContent);
        }

        var messages = new object[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user",   content = userMessage.ToString() }
        };

        // Azure OpenAI does NOT use response_format: json_object for plain-text output
        // and uses api-key header instead of Bearer
        var body = JsonSerializer.Serialize(new
        {
            model    = _model,
            messages
        }, _json);

        using var client = new HttpClient();

        if (_isAzure)
        {
            // Azure OpenAI: POST to {endpoint}/openai/deployments/{model}/chat/completions?api-version=...
            var endpoint = _baseUrl.TrimEnd('/');
            client.BaseAddress = new Uri($"{endpoint}/openai/deployments/{_model}/");
            client.DefaultRequestHeaders.Add("api-key", _apiKey);
        }
        else
        {
            client.BaseAddress = new Uri(_baseUrl.EndsWith('/') ? _baseUrl : _baseUrl + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        var requestUrl = _isAzure
            ? "chat/completions?api-version=2025-01-01-preview"
            : "chat/completions";

        using var response = await client.PostAsync(
            requestUrl,
            new StringContent(body, Encoding.UTF8, "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API error {(int)response.StatusCode}: {raw}");

        using var doc = JsonDocument.Parse(raw);
        var updatedHelperContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        // Save the updated helper with a unique timestamped name
        var outputDir = string.IsNullOrWhiteSpace(_helperOutputPath)
            ? Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory()
            : _helperOutputPath;

        Directory.CreateDirectory(outputDir);

        var timestamp      = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        var newHelperName  = $"CbrHelper_{timestamp}.js";
        var newHelperPath  = Path.Combine(outputDir, newHelperName);

        await File.WriteAllTextAsync(newHelperPath, updatedHelperContent, ct);

        // Extract change summary from the Topics line for console output
        var firstLine    = updatedHelperContent.Split('\n', 2)[0].Trim();
        var changeSummary = firstLine.StartsWith("// Topics:")
            ? firstLine["// Topics:".Length..].Trim()
            : $"Helper updated — {newHelperName}";

        return new CbrPassResult
        {
            UpdatedHelperPath = newHelperPath,
            ChangeSummary     = changeSummary
        };
    }

    /// <summary>
    /// Analyzes a single JS file to determine whether it contains functionality
    /// related to the given topic. If matching functions are found, their signatures
    /// (name, parameters, return type — no implementation) are appended to
    /// <c>&lt;OutputPath&gt;\&lt;topic&gt;.txt</c>, creating the file if it does not exist.
    /// </summary>
    /// <param name="topic">The topic to search for (e.g. "service filtering").</param>
    /// <param name="jsFilePath">Path to the JS file to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if matching functionality was found and written; otherwise false.</returns>
    public async Task<bool> AnalyzeTopicAsync(
        string topic,
        string jsFilePath,
        CancellationToken ct = default)
    {
        var fileName    = Path.GetFileName(jsFilePath);
        var fileContent = await File.ReadAllTextAsync(jsFilePath, ct);

        var systemPrompt =
            $$"""
            You are an expert JavaScript analyst for ShipExec ClientBusinessRules (CBR) files.

            Determine whether any functions in the provided JS file contain functionality
            related to the topic: "{{topic}}".

            If matching functionality exists, for each relevant function provide:
            1. A concise JavaScript-style signature: name, parameters with names, and a
               return-value comment.
            2. The complete, verbatim implementation of that function exactly as it appears
               in the source file — do not paraphrase or summarise it.

            Respond with JSON only, using exactly this shape:
            {
              "hasMatch": true,
              "methods": [
                {
                  "signature": "functionName(paramName, paramName2) // returns <description>",
                  "implementation": "function functionName(paramName, paramName2) { ... }"
                }
              ]
            }

            If there is no relevant functionality, respond with:
            {
              "hasMatch": false,
              "methods": []
            }

            No explanation or markdown outside the JSON object.
            """;

        var userMessage =
            $"""
            Topic: {topic}
            File: {fileName}

            === FILE CONTENT ===
            {fileContent}
            """;

        var messages = new object[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user",   content = userMessage }
        };

        var bodyObj = _isAzure
            ? (object)new { model = _model, messages }
            : new { model = _model, messages, response_format = new { type = "json_object" } };

        var body = JsonSerializer.Serialize(bodyObj, _json);

        using var client = new HttpClient();

        if (_isAzure)
        {
            var endpoint = _baseUrl.TrimEnd('/');
            client.BaseAddress = new Uri($"{endpoint}/openai/deployments/{_model}/");
            client.DefaultRequestHeaders.Add("api-key", _apiKey);
        }
        else
        {
            client.BaseAddress = new Uri(_baseUrl.EndsWith('/') ? _baseUrl : _baseUrl + "/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        var requestUrl = _isAzure
            ? "chat/completions?api-version=2025-01-01-preview"
            : "chat/completions";

        using var response = await client.PostAsync(
            requestUrl,
            new StringContent(body, Encoding.UTF8, "application/json"),
            ct);

        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API error {(int)response.StatusCode}: {raw}");

        using var doc = JsonDocument.Parse(raw);
        var aiContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        using var resultDoc = JsonDocument.Parse(aiContent);
        var root = resultDoc.RootElement;

        if (!root.GetProperty("hasMatch").GetBoolean())
            return false;

        var methods = root.GetProperty("methods");
        if (methods.GetArrayLength() == 0)
            return false;

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeTopic    = string.Concat(topic.Select(c => invalidChars.Contains(c) ? '_' : c));

        var outputPath = Path.Combine(_helperOutputPath, $"{safeTopic}.txt");
        Directory.CreateDirectory(_helperOutputPath);

        var sb = new StringBuilder();
        sb.AppendLine($"// Source: {fileName}");
        foreach (var method in methods.EnumerateArray())
        {
            var sig  = method.GetProperty("signature").GetString();
            var impl = method.GetProperty("implementation").GetString();

            if (!string.IsNullOrWhiteSpace(sig))
                sb.AppendLine($"// {sig}");

            if (!string.IsNullOrWhiteSpace(impl))
                sb.AppendLine(impl);

            sb.AppendLine();
        }

        await File.AppendAllTextAsync(outputPath, sb.ToString(), ct);
        return true;
    }

    /// <summary>
    /// Scans all files in the specified folder and collects topic names from the
    /// first line of each file. The first line is expected to be in the format:
    /// <c>// Topics: Topic1, Topic2, ...</c>
    /// </summary>
    /// <param name="folderPath">Path to the folder containing helper JS files to scan.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of all topic names found across all files in the folder.</returns>
    public static async Task<string> CollectTopicsFromFolderAsync(
        string folderPath,
        CancellationToken ct = default)
    {
        const string prefix = "// Topics: ";
        var topics = "";
        StringBuilder sb = new();

        foreach (var filePath in Directory.EnumerateFiles(folderPath))
        {
            ct.ThrowIfCancellationRequested();

            string? firstLine;
            using (var reader = new StreamReader(filePath))
                firstLine = await reader.ReadLineAsync(ct);

            if (string.IsNullOrWhiteSpace(firstLine) || !firstLine.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            foreach (var topic in firstLine[prefix.Length..].Split(','))
            {
                var trimmed = topic.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    sb.Append(trimmed + ",");
            }
        }

        return sb.ToString().TrimEnd(',');
    }
}
