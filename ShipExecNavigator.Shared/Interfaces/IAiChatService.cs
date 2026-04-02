namespace ShipExecNavigator.Shared.Interfaces;

public interface IAiChatService
{
    Task<string> SendMessageAsync(IReadOnlyList<ChatMessage> history, string userMessage, string? xmlContext = null, bool useRag = true, CancellationToken ct = default);
}

public record ChatMessage(string Role, string Content);
