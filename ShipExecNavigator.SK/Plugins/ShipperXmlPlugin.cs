using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ShipExecNavigator.SK.Plugins;

/// <summary>
/// Semantic Kernel plugin that analyses the loaded ShipExec XML and surfaces
/// shipper data so the AI can produce reasoning and a browser JS method.
/// </summary>
public sealed class ShipperXmlPlugin
{
    private readonly string _xmlContent;

    public ShipperXmlPlugin(string xmlContent)
    {
        _xmlContent = xmlContent;
    }

    [KernelFunction("find_shippers")]
    [Description(
        "Searches the loaded ShipExec XML configuration for Shipper entries. " +
        "Returns a structured report of matching shipper names and DOM layout hints " +
        "so you can produce reasoning and a JavaScript method to manipulate them in the Navigator tree view.")]
    public async Task<string> FindShippers(
        Kernel kernel,
        [Description("The current HTML content of the Navigator tree view")] string currentHTML,
        [Description("The user's request describing what to do with the shippers")] string userRequest)
    {
        // Kernel is injected automatically by SK when the function is invoked.
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var chatHistory = new ChatHistory(
            "Use this XML: " + _xmlContent +
            " and return a JSON object with properties: " +
            "1. confidenceLevel 2. reasoning 3. changesToBeApplied 4. textToDisplayToUser");

        chatHistory.AddUserMessage("Process this request: " + userRequest + ".  Think hard.");

        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await chatService.GetChatMessageContentAsync(
            chatHistory, executionSettings, kernel);



        var q = "Why isn't shipper with id 17778 in this list?";
        chatHistory.AddDeveloperMessage(q);

        var result2 = await chatService.GetChatMessageContentAsync(
            chatHistory, new AzureOpenAIPromptExecutionSettings(), kernel);

        return result.Content ?? "(empty response)";
    }
}

