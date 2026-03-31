using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using ShipExecNavigator.SK.Plugins;
using ShipExecNavigator.Shared.Interfaces;
using AppChatMessage = ShipExecNavigator.Shared.Interfaces.ChatMessage;

namespace ShipExecNavigator.SK;

public sealed class SemanticKernelChatService : IAiChatService
{
    private readonly IConfiguration _configuration;
    private readonly IVectorSearchService _vectorSearch;

    public SemanticKernelChatService(IConfiguration configuration, IVectorSearchService vectorSearch)
    {
        _configuration = configuration;
        _vectorSearch  = vectorSearch;
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

        var systemPrompt =
            "You are a helpful assistant for ShipExec Navigator, a tool for managing ShipExec " +
            "shipping software configuration. You help users understand company configuration XML, " +
            "users, roles, permissions, templates, and ShipExec concepts.";

        if (useRag)
            systemPrompt +=
                "\n\nYou have access to a document search tool. Call the search_documents function " +
                "whenever the user asks about ShipExec features, configuration options, or concepts " +
                "that may be covered in the documentation.";

        var hasXml = !string.IsNullOrWhiteSpace(xmlContext);
        if (hasXml)
            systemPrompt +=
                "\n\nThe user has a ShipExec XML configuration loaded in the Navigator. " +
                "When asked to hide, filter, or manipulate shippers or other XML elements, " +
                "call the available ShipperXml plugin functions to analyse the XML. " +
                "After receiving the plugin result always respond with exactly two sections:\n" +
                "1. **Reasoning:** Explain which items were found and why they match the criteria.\n" +
                "2. **JavaScript Method:** Provide a self-contained JavaScript function the user " +
                "can paste into the browser console to hide those elements in the Navigator tree view.";

        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey)
            .Build();

        if (hasXml)
            kernel.ImportPluginFromObject(new ShipperXmlPlugin(xmlContext!), "ShipperXml");

        if (useRag)
            kernel.ImportPluginFromObject(new RagSearchPlugin(_vectorSearch), "RagSearch");

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
            return result.Content ?? "(empty response)";
        }
        catch (Exception ex)
        {
            return $"⚠️ Azure OpenAI error: {ex.Message}";
        }
    }
}
