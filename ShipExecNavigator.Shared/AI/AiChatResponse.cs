using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShipExecNavigator.Shared.AI;

public class AiChatResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public AiChatAction? Action { get; set; }
}

public class AiChatAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; set; }
}
