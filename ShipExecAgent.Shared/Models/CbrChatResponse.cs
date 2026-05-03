namespace ShipExecAgent.Shared.Models;

public class CbrChatResponse
{
    /// <summary>The AI's conversational reply shown in the chat bubble.</summary>
    public string  Message { get; set; } = string.Empty;

    /// <summary>
    /// When the AI provides a full revised CBR script this field is populated.
    /// The UI shows an "Apply to script" button so the user can accept it.
    /// </summary>
    public string? Code    { get; set; }
}
