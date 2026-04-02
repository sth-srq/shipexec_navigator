using System.ComponentModel;
using System.Text.Json;
using System.Xml.Linq;
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

        var chatHistory = new ChatHistory();

        chatHistory.AddUserMessage("Process this request: " + userRequest + ". Using: " + _xmlContent + " --- Think hard.");

        var executionSettings = new AzureOpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
        };

        var result = await chatService.GetChatMessageContentAsync(
            chatHistory, executionSettings, kernel);

        return result.Content ?? "(empty response)";
    }

    [KernelFunction("delete_shippers")]
    [Description(
        "Extracts all Shipper entries from the loaded ShipExec XML configuration and returns them " +
        "as a JSON array with id, symbol, and name. Use this when the user asks to DELETE or REMOVE " +
        "shippers (not just hide). You MUST then filter the returned list to find matching shippers " +
        "and respond with a ```shipper-delete code block containing a JSON array of the matching entries.")]
    public string DeleteShippers(
        [Description("The user's request describing which shippers to delete")] string userRequest)
    {
        try
        {
            var doc = XDocument.Parse(_xmlContent);
            var shippers = doc.Descendants("Shipper")
                .Select(el => new
                {
                    id     = (string?)el.Element("Id")     ?? "",
                    symbol = (string?)el.Element("Symbol") ?? "",
                    name   = (string?)el.Element("Name")   ?? ""
                })
                .Where(s => !string.IsNullOrEmpty(s.id))
                .ToList();

            var json = JsonSerializer.Serialize(shippers, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {shippers.Count} shipper(s) in the XML:\n{json}\n\n" +
                   "Filter this list based on the user's condition and respond with a " +
                   "```shipper-delete code block containing ONLY the JSON array of matching entries " +
                   "(each with id, symbol, name).";
        }
        catch (Exception ex)
        {
            return $"Failed to parse XML: {ex.Message}";
        }
    }
}

