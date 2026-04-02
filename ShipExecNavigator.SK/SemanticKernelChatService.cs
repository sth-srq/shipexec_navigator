using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ShipExecNavigator.SK.Plugins;
using ShipExecNavigator.Shared.AI;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Logging;
using AppChatMessage = ShipExecNavigator.Shared.Interfaces.ChatMessage;

namespace ShipExecNavigator.SK;

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

    public async Task<string> SendMessageAsync(
        IReadOnlyList<AppChatMessage> history,
        string userMessage,
        string? xmlContext = null,
        bool useRag = true,
        CancellationToken ct = default)
    {
        var endpoint   = _configuration["AzureOpenAI:Endpoint"]        ?? string.Empty;
        var apiKey     = _configuration["AzureOpenAI:ApiKey"]           ?? string.Empty;
        var deployment = _configuration["AzureOpenAI:ChatDeployment"]   ?? "gpt-4o-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
            return "⚠️ Azure OpenAI is not configured. Set `AzureOpenAI:ApiKey` in appsettings.json.";

        var sw      = System.Diagnostics.Stopwatch.StartNew();
        var preview = userMessage.Length > 200 ? userMessage[..200] + "..." : userMessage;
        var hasXml  = !string.IsNullOrWhiteSpace(xmlContext);
        _logger.LogTrace(">> SendMessageAsync | Deployment={Deployment} UseRag={UseRag} HasXml={HasXml} History={History}",
            deployment, useRag, hasXml, history.Count);
        _logger.LogInformation(
            "AzureOpenAI request | Deployment={Deployment} UseRag={UseRag} HasXmlContext={HasXml} HistoryCount={History} MessagePreview={Preview}",
            deployment, useRag, hasXml, history.Count, preview);

        var systemPrompt =
            "You are a helpful assistant for ShipExec Navigator, a tool for managing ShipExec " +
            "shipping software configuration. You help users understand company configuration XML, " +
            "users, roles, permissions, templates, and ShipExec concepts.";

        if (useRag)
            systemPrompt +=
                "\n\nYou have access to a document search tool. Call the search_documents function " +
                "whenever the user asks about ShipExec features, configuration options, or concepts " +
                "that may be covered in the documentation.";

        if (hasXml)
            systemPrompt +=
                "\n\nThe user has a ShipExec XML configuration loaded in the Navigator. " +
                "When asked to hide, filter, or manipulate shippers or other XML elements, " +
                "call the available ShipperXml plugin functions to analyse the XML. " +
                "After receiving the plugin result always respond with exactly two sections:\n" +
                "1. **Reasoning:** Explain which items were found and why they match the criteria.\n" +
                "2. **JavaScript Method:** Provide a self-contained JavaScript function the user " +
                "can paste into the browser console to hide those elements in the Navigator tree view.\n\n" +
                "**Deleting shippers:** When the user asks to DELETE or REMOVE shippers (not just hide), " +
                "call the `delete_shippers` function. After receiving the shipper list, filter it to match " +
                "the user's condition, then respond with:\n" +
                "1. **Reasoning:** Explain which shippers matched and will be deleted.\n" +
                "2. A ```shipper-delete code block containing a JSON array of the matching entries " +
                "(each object must have `id`, `symbol`, and `name` string fields, exactly as returned by the plugin). " +
                "Do NOT include any JavaScript for deletion — the Navigator will process the shipper-delete block directly.\n\n" +
                "**Editing shippers:** When the user asks to UPDATE, EDIT, SET, or CHANGE field values on " +
                "shippers (with optional conditions like 'for all shippers that start with T'), " +
                "call the `edit_shippers` function. After receiving the full shipper list, filter it to match " +
                "the user's condition, then respond with:\n" +
                "1. **Reasoning:** Explain which shippers matched and what fields will be changed.\n" +
                "2. A ```shipper-edit code block containing a JSON array where each object has " +
                "`id` (string), `symbol` (string), `name` (string), and `edits` (an object mapping " +
                "field names to new string values). Valid field names: Name, Symbol, Code, Address1, " +
                "Address2, Address3, City, StateProvince, PostalCode, Country, Company, Contact, Phone, " +
                "Fax, Email, Sms, PoBox, Residential. " +
                "Do NOT include any JavaScript — the Navigator will process the shipper-edit block directly.\n" +
                NavigatorDomCheatSheet.Content;

        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey)
            .Build();

        if (hasXml)
            kernel.ImportPluginFromObject(new ShipperXmlPlugin(xmlContext!), "ShipperXml");

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
        if (hasXml || useRag)
            executionSettings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

        try
        {
            var result = await chatService.GetChatMessageContentAsync(
                chatHistory, executionSettings, kernel, ct);
            var content = result.Content ?? "(empty response)";
            sw.Stop();
            _logger.LogInformation(
                "AzureOpenAI response | Deployment={Deployment} ResponseLength={Length} DurationMs={DurationMs}",
                deployment, content.Length, sw.ElapsedMilliseconds);
            return content;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "AzureOpenAI error | Deployment={Deployment} DurationMs={DurationMs}",
                deployment, sw.ElapsedMilliseconds);
            return $"⚠️ Azure OpenAI error: {ex.Message}";
        }
    }
}
