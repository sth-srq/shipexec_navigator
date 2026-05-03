using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ShipExecAgent.SK.Plugins;
using ShipExecAgent.Shared.AI;
using ShipExecAgent.Shared.Interfaces;
using ShipExecAgent.Shared.Logging;
using ShipExecAgent.Shared.Models;
using AppChatMessage = ShipExecAgent.Shared.Interfaces.ChatMessage;

namespace ShipExecAgent.SK;

/// <summary>
/// Azure OpenAI–backed chat service that uses Microsoft Semantic Kernel with
/// optional Retrieval-Augmented Generation (RAG) and XML-analysis plugins.
/// <para>
/// <b>Plugin wiring:</b>
/// <list type="bullet">
///   <item>
///     <term><c>RagSearch / search_documents</c></term>
///     <description>
///       Enabled when <paramref name="useRag"/> is <see langword="true"/> (default).
///       The kernel calls <see cref="RagSearchPlugin"/> to search the local vector index
///       for ShipExec documentation relevant to the user's question.
///     </description>
///   </item>
///   <item>
///     <term><c>ShipperXml / find_shippers</c></term>
///     <description>
///       Enabled when the caller provides a non-empty <paramref name="xmlContext"/>.
///       The kernel calls <see cref="ShipperXmlPlugin"/> to analyse the loaded company
///       XML and surface structured shipper data for reasoning.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Configuration keys</b> (read from <c>appsettings.json</c> / environment):
/// <list type="table">
///   <listheader><term>Key</term><description>Description</description></listheader>
///   <item><term><c>AzureOpenAI:Endpoint</c></term><description>Azure OpenAI resource endpoint URL.</description></item>
///   <item><term><c>AzureOpenAI:ApiKey</c></term><description>Azure OpenAI API key.</description></item>
///   <item><term><c>AzureOpenAI:ChatDeployment</c></term><description>Deployment name (defaults to <c>gpt-4o-mini</c>).</description></item>
/// </list>
/// </para>
/// <para>
/// When <c>AzureOpenAI:ApiKey</c> is empty the method returns a user-visible warning
/// string rather than throwing, so the AI panel degrades gracefully in environments
/// where OpenAI is not configured.
/// </para>
/// </summary>
public sealed class SemanticKernelChatService : IAiChatService
{
    private readonly IConfiguration _configuration;
    private readonly IVectorSearchService _vectorSearch;
    private readonly ILogger<SemanticKernelChatService> _logger;

    public SemanticKernelChatService(
        IConfiguration configuration,
        IVectorSearchService vectorSearch,
        ILogger<SemanticKernelChatService> logger)
    {
        _configuration = configuration;
        _vectorSearch  = vectorSearch;
        _logger        = logger;
    }

    public async Task<AiChatResponse> SendMessageAsync(
        IReadOnlyList<AppChatMessage> history,
        string userMessage,
        string? xmlContext = null,
        bool useRag = true,
        string? usersContext = null,
        string? userMetaContext = null,
        string? cbrsContext = null,
        string? logsContext = null,
        CompanyEntityIndex? entityIndex = null,
        CancellationToken ct = default)
    {
        var endpoint   = _configuration["AzureOpenAI:Endpoint"]        ?? string.Empty;
        var apiKey     = _configuration["AzureOpenAI:ApiKey"]           ?? string.Empty;
        var deployment = _configuration["AzureOpenAI:ChatDeployment"]   ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiChatResponse { Message = "⚠️ Azure OpenAI is not configured. Set `AzureOpenAI:ApiKey` in appsettings.json." };

        var sw      = System.Diagnostics.Stopwatch.StartNew();
        var preview = userMessage.Length > 200 ? userMessage[..200] + "..." : userMessage;
        var hasXml  = !string.IsNullOrWhiteSpace(xmlContext);
        var hasIndex = entityIndex is not null;
        var hasUsers = !string.IsNullOrWhiteSpace(usersContext);
        var hasCbrs  = !string.IsNullOrWhiteSpace(cbrsContext);
        var hasLogs  = !string.IsNullOrWhiteSpace(logsContext);
        _logger.LogTrace(">> SendMessageAsync | Deployment={Deployment} UseRag={UseRag} HasXml={HasXml} HasIndex={HasIndex} HasUsers={HasUsers} HasCbrs={HasCbrs} HasLogs={HasLogs} History={History}",
            deployment, useRag, hasXml, hasIndex, hasUsers, hasCbrs, hasLogs, history.Count);
        _logger.LogInformation(
            "AzureOpenAI request | Deployment={Deployment} UseRag={UseRag} HasXmlContext={HasXml} HasEntityIndex={HasIndex} HasUsersContext={HasUsers} HasCbrsContext={HasCbrs} HasLogsContext={HasLogs} HistoryCount={History} MessagePreview={Preview}",
            deployment, useRag, hasXml, hasIndex, hasUsers, hasCbrs, hasLogs, history.Count, preview);

        var systemPrompt =
            "You are ShipExec Copilot — an enthusiastic, knowledgeable assistant for ShipExec Navigator, a tool for managing " +
            "ShipExec shipping software configuration. You help users understand company configuration XML, " +
            "users, roles, permissions, templates, and ShipExec concepts. " +
            "Be friendly and upbeat in your responses. Use an encouraging, positive tone. " +
            "When a user's request is ambiguous or could be interpreted multiple ways, ask a brief clarifying question " +
            "before proceeding — for example: \"Just to make sure I get this right — did you mean X or Y?\" " +
            "After completing a task or answering a question, suggest logical next steps the user might want to take. " +
            "For example: \"Now that we've updated those shippers, would you like me to review the remaining ones " +
            "or export the changes?\" Keep suggestions concise — one or two short bullet points at most. " +
            "IMPORTANT: Always respond with a single valid JSON object — no markdown, no plain text, no code fences outside the JSON. " +
            "Use exactly this structure: " +
            "{ \"message\": \"<your reply to the user>\", \"action\": { \"type\": \"<type>\", \"payload\": <payload> } } " +
            "Omit the \"action\" key entirely when no action is needed. " +
            "Supported action types: javascript (payload: JS code string), " +
            "shipper-add (payload: object), shipper-delete (payload: array), shipper-edit (payload: array), " +
            "user-find (payload: array), user-add (payload: object), user-edit (payload: array), user-delete (payload: array), " +
            "adapter-registration-delete (payload: array of {id, name}), adapter-registration-edit (payload: array of {id, name, edits:{fieldName:value}}), " +
            "client-delete (payload: array of {id, name}), client-edit (payload: array of {id, name, edits:{fieldName:value}}), " +
            "entity-delete (payload: array of {entityType, id, name}), " +
            "entity-edit (payload: array of {entityType, id, name, edits:{fieldName:value}}), " +
            "cbr-edit (payload: {id, name, script}), " +
            "log-find (payload: array of {id (int), source (\"App\" or \"Security\")})." +
            "The \"message\" value inside the JSON must use rich Markdown formatting: " +
            "use **bold** for key terms, `code` for field names and values, ``` fenced code blocks for multi-line code or XML, " +
            "# / ## / ### headings to organise long answers, - bullet lists for enumerations, " +
            "1. numbered lists for steps, | tables for structured data comparisons, " +
            "and > blockquotes to highlight important notes or warnings. " +
            "Always prefer structured Markdown over plain prose for any non-trivial answer.";

        if (useRag)
            systemPrompt +=
                "\n\nYou have access to a document search tool. Call the search_documents function " +
                "whenever the user asks about ShipExec features, configuration options, or concepts " +
                "that may be covered in the documentation.";

        if (hasXml && !hasIndex)
            systemPrompt +=
                "\n\nThe user has a ShipExec XML configuration loaded in the Navigator. " +
                "When asked to hide, filter, or manipulate shippers or other XML elements, " +
                "call the available ShipperXml plugin functions to analyse the XML. " +
                "Respond using the JSON structure above.\n\n" +
                "**Hiding shippers (JavaScript):** When asked to visually hide/show elements, " +
                "respond with action type \"javascript\" and payload as the JS code string.\n\n" +
                "**Deleting shippers:** When the user asks to DELETE or REMOVE shippers (not just hide), " +
                "call the `delete_shippers` function. Filter the results, then " +
                "respond with action type \"shipper-delete\" and payload as a JSON array of matching entries " +
                "(each object must have `id`, `symbol`, and `name` string fields). " +
                "Do NOT use action type \"javascript\" for deletion.\n\n" +
                "**Editing shippers:** When the user asks to UPDATE, EDIT, SET, or CHANGE field values on shippers, " +
                "call the `edit_shippers` function. Filter the results, then " +
                "respond with action type \"shipper-edit\" and payload as a JSON array where each object has " +
                "`id` (string), `symbol` (string), `name` (string), and `edits` (an object mapping field names to new values). " +
                "Valid field names: Name, Symbol, Code, Address1, Address2, Address3, City, StateProvince, PostalCode, " +
                "Country, Company, Contact, Phone, Fax, Email, Sms, PoBox, Residential. " +
                "Do NOT use action type \"javascript\" for editing.\n\n" +
                "**Generic entity operations:** For ANY other entity type on the Navigator screen " +
                "(Profile, Site, CarrierRoute, ClientBusinessRule, DataConfigurationMapping, DocumentConfiguration, " +
                "Machine, PrinterConfiguration, PrinterDefinition, ScaleConfiguration, Schedule, " +
                "ServerBusinessRule, SourceConfiguration), use the EntityXml plugin functions:\n" +
                "- To DELETE/REMOVE: call `delete_entities` with the entity type, then respond with " +
                "action type \"entity-delete\" and payload as a JSON array where each object has " +
                "`entityType` (string), `id` (string), and `name` (string).\n" +
                "- To EDIT/UPDATE/SET/CHANGE: call `edit_entities` with the entity type, then respond with " +
                "action type \"entity-edit\" and payload as a JSON array where each object has " +
                "`entityType` (string), `id` (string), `name` (string), and `edits` (object mapping field names to new values).\n" +
                "- To inspect/list: call `find_entities` or `list_entity_types`.\n" +
                "Do NOT use action type \"javascript\" for entity deletion or editing.\n\n" +
                "**Profile composition:** Profiles are composed of other entities. " +
                "When a user asks whether something is IN a profile, USED BY a profile, or CONTAINED in a profile, " +
                "look for BOTH child elements (nested entities) AND ID reference fields " +
                "(e.g. `ClientBusinessRuleId` references an existing Client Business Rule, " +
                "`ServerBusinessRuleId` references a Server Business Rule, etc.). " +
                "Some of these referenced entities are defined at higher levels in the configuration hierarchy.\n" +
                NavigatorDomCheatSheet.Content;

        if (hasIndex)
            systemPrompt +=
                "\n\nThe user has a ShipExec company configuration loaded" +
                (string.IsNullOrWhiteSpace(entityIndex!.CompanyName) ? ". " : $" (company name: **{entityIndex.CompanyName}**). ") +
                "A complete entity index is available — ALL entity categories are indexed regardless of " +
                "which tree nodes the user has expanded in the UI.\n\n" +
                "**Entity manifest (categories and entities):**\n" + entityIndex!.ManifestJson + "\n\n" +
                "To get full field-level details for any category, call the `get_category_details` function.\n\n" +
                "**Shipper operations:**\n" +
                "- FIND/SEARCH: call `find_shippers`\n" +
                "- DELETE/REMOVE: call `delete_shippers`, then respond with action type \"shipper-delete\"\n" +
                "- EDIT/UPDATE: call `edit_shippers`, then respond with action type \"shipper-edit\"\n" +
                "- ADD/CREATE: respond with action type \"shipper-add\" (no plugin call needed)\n\n" +
                "**User operations:**\n" +
                "- FIND/SEARCH/COUNT/LIST: call `find_users` to get all users with their fields (Id, UserName, Email, ProfileId, ProfileName, etc.)\n" +
                "- Users are always available in the entity index even when the Users tab is not open\n\n" +
                "**Generic entity operations** (Profile, Site, CarrierRoute, ClientBusinessRule, " +
                "DataConfigurationMapping, DocumentConfiguration, Machine, PrinterConfiguration, " +
                "PrinterDefinition, ScaleConfiguration, Schedule, ServerBusinessRule, SourceConfiguration):\n" +
                "- FIND/INSPECT: call `find_entities` or `get_category_details`\n" +
                "- DELETE/REMOVE: call `delete_entities`, then respond with action type \"entity-delete\"\n" +
                "- EDIT/UPDATE: call `edit_entities`, then respond with action type \"entity-edit\"\n\n" +
                "Do NOT use action type \"javascript\" for entity operations.\n\n" +
                "**Profile composition & cross-referencing:** Profiles are composed of other entities. " +
                "When a user asks whether something is IN a profile, USED BY a profile, CONTAINED in a profile, " +
                "OR asks which entities are associated with which profiles (in either direction), " +
                "look for BOTH child elements (nested entities) AND ID reference fields " +
                "(e.g. `ClientBusinessRuleId`, `ServerBusinessRuleId`, `SiteId`, etc.). " +
                "Some of these referenced entities are defined at higher levels in the configuration hierarchy.\n" +
                "Call `get_category_details` for the relevant categories to resolve ID references.\n" +
                NavigatorDomCheatSheet.Content;

        if (hasUsers)
            systemPrompt +=
                "\n\n**Finding users:** When the user asks to FIND, SEARCH, or FILTER users, call the `find_users` function. " +
                "Respond with action type \"user-find\" and payload as a JSON array of matching entries " +
                "(each object must have `id`, `username`, and `email` string fields). " +
                "Do NOT use action type \"javascript\".\n\n" +
                "**Editing users:** When the user asks to UPDATE, EDIT, SET, or CHANGE ANY field on users, " +
                "call the `edit_users` function. Respond with action type \"user-edit\" and payload as a JSON array " +
                "where each object has `id` (string), `username` (string), `email` (string), and `edits` (an object). " +
                "Supported edit fields:\n" +
                "  - Top-level: `Email`, `UserName`, `PhoneNumber`, `PasswordExpired` (bool), `LockoutEnabled` (bool), `EmailConfirmed` (bool), `PhoneNumberConfirmed` (bool)\n" +
                "  - Address: `Address.Company`, `Address.Contact`, `Address.Address1`, `Address.Address2`, `Address.Address3`, `Address.City`, `Address.StateProvince`, `Address.PostalCode`, `Address.Country`, `Address.Phone`, `Address.Fax`, `Address.Email`, `Address.Sms`, `Address.Account`, `Address.TaxId`, `Address.Code`, `Address.Group`, `Address.PoBox` (bool), `Address.Residential` (bool)\n" +
                "  - Config: `Config.ExportFileDelimiter` (Comma/Semicolon/Tab), `Config.ExportFileQualifier` (None/DoubleQuotes/SingleQuote), `Config.ExportFileGroupSeparator` (Comma/Period), `Config.ExportFileDecimalSeparator` (Comma/Period)\n" +
                "  - Permissions: `Permissions.Add` (permission name), `Permissions.Remove` (permission name)\n" +
                "  - Roles: `Roles.Add` (role name), `Roles.Remove` (role name)\n" +
                "Do NOT use action type \"javascript\".\n\n" +
                "**Deleting users:** When the user asks to DELETE or REMOVE users, call the `delete_users` function. " +
                "Respond with action type \"user-delete\" and payload as a JSON array of matching entries " +
                "(each object must have `id`, `username`, and `email` string fields). " +
                "Do NOT use action type \"javascript\".\n\n" +
                "**Adding users:** When the user asks to ADD or CREATE a new user, do NOT call any plugin. " +
                "Respond with action type \"user-add\" and payload as a JSON object. " +
                "Required field: `email` (string). " +
                "Optional fields: `company`, `contact`, `address1`, `address2`, `address3`, `city`, `stateProvince`, `postalCode`, `country`, `phone`, `fax` (all strings). " +
                "Do NOT use action type \"javascript\".";

        if (hasLogs)
            systemPrompt +=
                "\n\n**Finding log entries:** When the user asks to FIND, SEARCH, FILTER, or SUMMARIZE log entries, " +
                "call the `find_logs` function. Filter the results, then " +
                "respond with action type \"log-find\" and payload as a JSON array of matching entries " +
                "(each object must have `id` (int) and `source` (\"App\" or \"Security\") fields). " +
                "Do NOT use action type \"javascript\".";

        if (hasCbrs)
            systemPrompt +=
                "\n\n**Editing Client Business Rules (CBR):** When the user asks to UPDATE, EDIT, SET, or CHANGE " +
                "the script of a client business rule, respond with action type \"cbr-edit\" and payload as a JSON object " +
                "with `id` (int), `name` (string), and `script` (string — the full updated script text). " +
                "Do NOT use action type \"javascript\".\n\n" +
                "Available CBRs (id, name, description, version):\n" + cbrsContext;

        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey)
            .Build();

        CompanyIndexPlugin? indexPlugin = null;
        if (hasIndex)
        {
            indexPlugin = new CompanyIndexPlugin(entityIndex!);
            kernel.ImportPluginFromObject(indexPlugin, "CompanyIndex");
        }
        else if (hasXml)
        {
            kernel.ImportPluginFromObject(new ShipperXmlPlugin(xmlContext!), "ShipperXml");
            kernel.ImportPluginFromObject(new EntityXmlPlugin(xmlContext!), "EntityXml");
        }

        if (hasUsers)
            kernel.ImportPluginFromObject(new UserXmlPlugin(usersContext!, userMetaContext ?? "{}"), "UserXml");

        if (hasLogs)
            kernel.ImportPluginFromObject(new LogsPlugin(logsContext!), "Logs");

        if (useRag)
            kernel.ImportPluginFromObject(
                new RagSearchPlugin(_vectorSearch, AppLoggerFactory.CreateLogger<RagSearchPlugin>()), "RagSearch");

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory(systemPrompt);

        foreach (var msg in history)
        {
            if (msg.Role == "user")
                chatHistory.AddUserMessage(msg.Content);
            else if (msg.Role == "assistant")
                chatHistory.AddAssistantMessage(msg.Content);
        }

        chatHistory.AddUserMessage(userMessage);

        var executionSettings = new AzureOpenAIPromptExecutionSettings();
        if (hasXml || hasIndex || hasUsers || useRag || hasCbrs || hasLogs)
            executionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

        try
        {
            var result = await chatService.GetChatMessageContentAsync(
                chatHistory, executionSettings, kernel, ct);
            var content = result.Content ?? "(empty response)";
            sw.Stop();

            var accessedCategories = indexPlugin?.AccessedCategories.ToList() ?? [];
            if (accessedCategories.Count > 0)
                _logger.LogInformation(
                    "AzureOpenAI response | Deployment={Deployment} ResponseLength={Length} DurationMs={DurationMs} AccessedCategories={Categories}",
                    deployment, content.Length, sw.ElapsedMilliseconds, string.Join(", ", accessedCategories));
            else
                _logger.LogInformation(
                    "AzureOpenAI response | Deployment={Deployment} ResponseLength={Length} DurationMs={DurationMs}",
                    deployment, content.Length, sw.ElapsedMilliseconds);

            try
            {
                var response = System.Text.Json.JsonSerializer.Deserialize<AiChatResponse>(content,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
                    ?? new AiChatResponse { Message = content };
                response.ReferencedCategories = accessedCategories;
                return response;
            }
            catch (System.Text.Json.JsonException)
            {
                _logger.LogWarning("SK response was not valid JSON; treating as plain text.");
                return new AiChatResponse { Message = content, ReferencedCategories = accessedCategories };
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "AzureOpenAI error | Deployment={Deployment} DurationMs={DurationMs}",
                deployment, sw.ElapsedMilliseconds);
            return new AiChatResponse { Message = "⚠️ Azure OpenAI error: check server logs for details." + " _ " + ex.Message + " _ " + ex.StackTrace};
        }
    }
}
