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

    public async Task<AiChatResponse> SendMessageAsync(IReadOnlyList<ChatMessage> history, string userMessage, string? xmlContext = null, bool useRag = true, string? usersContext = null, string? userMetaContext = null, string? cbrsContext = null, string? logsContext = null, CancellationToken ct = default)
    {
        var apiKey  = configuration["AiChat:ApiKey"]  ?? string.Empty;
        var baseUrl = configuration["AiChat:BaseUrl"]  ?? "https://api.openai.com/v1/";
        var model   = configuration["AiChat:Model"]    ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiChatResponse { Message = "⚠️ No AI API key configured. Add `AiChat:ApiKey` to appsettings.json." };

        logger.LogTrace(">> SendMessageAsync | Model={Model} History={History}", model, history.Count);
        var sw      = System.Diagnostics.Stopwatch.StartNew();
        var preview = userMessage.Length > 200 ? userMessage[..200] + "..." : userMessage;
        logger.LogInformation(
            "OpenAI request | Model={Model} BaseUrl={BaseUrl} HistoryCount={History} MessagePreview={Preview}",
            model, baseUrl, history.Count, preview);

        var messages = new List<object>();

        var systemContent = new StringBuilder(
            "You are a helpful assistant for ShipExec Navigator. " +
            "IMPORTANT: Always respond with a single valid JSON object — no markdown, no plain text, no code fences outside the JSON. " +
            "Use exactly this structure: " +
            "{ \"message\": \"<your reply to the user>\", \"action\": { \"type\": \"<type>\", \"payload\": <payload> } } " +
            "Omit the \"action\" key entirely when no action is needed. " +
            "Supported action types: javascript (payload: JS code string), " +
            "shipper-add (payload: ShipperAddItem object), shipper-delete (payload: array), " +
            "shipper-edit (payload: array), user-find (payload: array), " +
            "user-add (payload: object), user-edit (payload: array), user-delete (payload: array), " +
            "adapter-registration-delete (payload: array of {id, name}), " +
            "adapter-registration-edit (payload: array of {id, name, edits: {fieldName: newValue}}), " +
            "client-delete (payload: array of {id, name}), " +
            "client-edit (payload: array of {id, name, edits: {fieldName: newValue}}), " +
            "entity-delete (payload: array of {entityType, id, name}), " +
            "entity-edit (payload: array of {entityType, id, name, edits: {fieldName: newValue}}), " +
            "cbr-edit (payload: {id, name, script}), " +
            "log-find (payload: array of {id (int), source (\"App\" or \"Security\")}).");

        if (!string.IsNullOrWhiteSpace(xmlContext))
        {
            systemContent.Append(
                " The user has a ShipExec XML configuration loaded. " +
                "When writing JavaScript to manipulate the Navigator tree view, " +
                "use only the class names and IDs documented in the DOM reference below.");
            systemContent.Append(
                "\n\n**Generic entity operations:** For ANY entity type on the Navigator screen " +
                "(Profile, Site, CarrierRoute, ClientBusinessRule, DataConfigurationMapping, DocumentConfiguration, " +
                "Machine, PrinterConfiguration, PrinterDefinition, ScaleConfiguration, Schedule, " +
                "ServerBusinessRule, SourceConfiguration), you can:\n" +
                "- DELETE/REMOVE: respond with action type \"entity-delete\" and payload as a JSON array " +
                "where each object has `entityType` (singular, e.g. \"Profile\"), `id` (string), and `name` (string).\n" +
                "- EDIT/UPDATE/SET/CHANGE: respond with action type \"entity-edit\" and payload as a JSON array " +
                "where each object has `entityType` (singular, e.g. \"Profile\"), `id` (string), `name` (string), " +
                "and `edits` (object mapping field names to new values).\n" +
                "Do NOT use action type \"javascript\" for entity deletion or editing.");
            systemContent.Append(
                "\n\n**Profile composition & cross-referencing:** Profiles are composed of other entities. " +
                "When a user asks whether something is IN a profile, USED BY a profile, CONTAINED in a profile, " +
                "OR asks which entities are associated with which profiles (in either direction), " +
                "look for BOTH child elements (nested entities) AND ID reference fields " +
                "(e.g. `ClientBusinessRuleId`, `ServerBusinessRuleId`, `SiteId`, etc.). " +
                "Some of these referenced entities are defined at higher levels in the configuration hierarchy.\n\n" +
                "**How to resolve ID references:** Many entities reference each other by ID fields. " +
                "To answer questions about relationships between entities, follow these steps:\n" +
                "1. Find each entity of the first type (e.g. each `<Profile>` under `<Profiles>`).\n" +
                "2. Read the ID reference field (e.g. the `<ClientBusinessRuleId>` child element's text value).\n" +
                "3. Find the matching entity of the referenced type whose `<Id>` child element matches that value " +
                "(e.g. find the `<ClientBusinessRule>` under `<ClientBusinessRules>` whose `<Id>` equals the value from step 2).\n" +
                "4. Return the referenced entity's `<Name>` (or other identifying field) alongside the referring entity's name.\n" +
                "5. If the ID reference field is empty, missing, or zero, report that the entity has no reference set.\n" +
                "6. Repeat for every entity of the first type.\n\n" +
                "Common ID reference fields on Profiles: `ClientBusinessRuleId` → `ClientBusinessRule`, " +
                "`ServerBusinessRuleId` → `ServerBusinessRule`, `SiteId` → `Site`, " +
                "`DocumentConfigurationId` → `DocumentConfiguration`, `SourceConfigurationId` → `SourceConfiguration`, " +
                "`PrinterConfigurationId` → `PrinterConfiguration`, `ScaleConfigurationId` → `ScaleConfiguration`.");
            systemContent.Append(NavigatorDomCheatSheet.Content);
        }

        if (!string.IsNullOrWhiteSpace(usersContext))
        {
            systemContent.Append(
                "\n\nThe user has a ShipExec users list loaded. " +
                "Here are the available users (JSON):\n" + usersContext + "\n\n" +
                "**Finding users:** When asked to FIND, SEARCH, or FILTER users, " +
                "respond with action type \"user-find\" and payload as a JSON array of matching entries " +
                "(each object must have `id`, `username`, and `email` string fields). " +
                "Do NOT use action type \"javascript\".\n\n" +
                "**Editing users:** When asked to UPDATE, EDIT, SET, or CHANGE ANY field on users, " +
                "respond with action type \"user-edit\" and payload as a JSON array where each object has " +
                "`id` (string), `username` (string), `email` (string), and `edits` (an object). " +
                "Supported edit fields:\n" +
                "  - Top-level: `Email`, `UserName`, `PhoneNumber`, `PasswordExpired` (bool), `LockoutEnabled` (bool), `EmailConfirmed` (bool), `PhoneNumberConfirmed` (bool)\n" +
                "  - Address: `Address.Company`, `Address.Contact`, `Address.Address1`, `Address.Address2`, `Address.Address3`, `Address.City`, `Address.StateProvince`, `Address.PostalCode`, `Address.Country`, `Address.Phone`, `Address.Fax`, `Address.Email`, `Address.Sms`, `Address.Account`, `Address.TaxId`, `Address.Code`, `Address.Group`, `Address.PoBox` (bool), `Address.Residential` (bool)\n" +
                "  - Config: `Config.ExportFileDelimiter` (Comma/Semicolon/Tab), `Config.ExportFileQualifier` (None/DoubleQuotes/SingleQuote), `Config.ExportFileGroupSeparator` (Comma/Period), `Config.ExportFileDecimalSeparator` (Comma/Period)\n" +
                "  - Permissions: `Permissions.Add` (permission name), `Permissions.Remove` (permission name)\n" +
                "  - Roles: `Roles.Add` (role name), `Roles.Remove` (role name)\n" +
                "Do NOT use action type \"javascript\".\n\n" +
                "**Deleting users:** When asked to DELETE or REMOVE users, " +
                "respond with action type \"user-delete\" and payload as a JSON array of matching entries " +
                "(each object must have `id`, `username`, and `email` string fields). " +
                "Do NOT use action type \"javascript\".\n\n" +
                "**Adding users:** When asked to ADD or CREATE a new user, " +
                "respond with action type \"user-add\" and payload as a JSON object. " +
                "Required field: `email` (string). " +
                "Optional fields: `company`, `contact`, `address1`, `address2`, `address3`, `city`, `stateProvince`, `postalCode`, `country`, `phone`, `fax` (all strings). " +
                "Do NOT use action type \"javascript\".");

            if (!string.IsNullOrWhiteSpace(userMetaContext))
            {
                systemContent.Append(
                    "\n\nAvailable permissions and roles for this company:\n" + userMetaContext);
            }
        }

        if (!string.IsNullOrWhiteSpace(logsContext))
        {
            systemContent.Append(
                "\n\n**Finding log entries:** When the user asks to FIND, SEARCH, FILTER, or SUMMARIZE log entries, " +
                "respond with action type \"log-find\" and payload as a JSON array of matching entries " +
                "(each object must have `id` (int) and `source` (\"App\" or \"Security\") fields). " +
                "Do NOT use action type \"javascript\"." +
                "\n\nLoaded log entries (JSON):\n" + logsContext);
        }

        if (!string.IsNullOrWhiteSpace(cbrsContext))
        {
            systemContent.Append(
                " The user has the following Client Business Rules (CBRs) loaded: " +
                cbrsContext +
                " For the cbr-edit action, set the payload to {\"id\": <int id>, \"name\": \"<rule name>\", \"script\": \"<complete new JavaScript script>\"}.");
        }

        messages.Add(new { role = "system", content = systemContent.ToString() });

        messages.AddRange(history
            .Select(m => (object)new { role = m.Role, content = m.Content }));
        messages.Add(new { role = "user", content = userMessage });

        var body = JsonSerializer.Serialize(new
        {
            model,
            messages,
            response_format = new { type = "json_object" }
        }, _json);

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
            return new AiChatResponse { Message = $"⚠️ API error {(int)response.StatusCode}: check server logs for details." };
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

        try
        {
            return JsonSerializer.Deserialize<AiChatResponse>(content, _json)
                   ?? new AiChatResponse { Message = content };
        }
        catch (JsonException)
        {
            logger.LogWarning("AI response was not valid JSON; treating as plain text.");
            return new AiChatResponse { Message = content };
        }
    }
}
