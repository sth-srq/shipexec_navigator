using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShipExecNavigator.Shared.AI;

public class AiChatResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public AiChatAction? Action { get; set; }

    /// <summary>
    /// Entity categories that the AI accessed during its function-calling loop
    /// (e.g. "Shippers", "Profiles").  The UI uses this to auto-expand the
    /// corresponding tree nodes so the user can see the referenced data.
    /// This is populated server-side and never sent by the LLM.
    /// </summary>
    [JsonIgnore]
    public List<string> ReferencedCategories { get; set; } = [];
}

public class AiChatAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }
}
