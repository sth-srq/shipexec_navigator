using ShipExecNavigator.Shared.AI;

namespace ShipExecNavigator.Shared.Interfaces;

public interface IAiChatService
{
    Task<AiChatResponse> SendMessageAsync(IReadOnlyList<ChatMessage> history, string userMessage, string? xmlContext = null, bool useRag = true, string? usersContext = null, string? userMetaContext = null, CancellationToken ct = default);
}

public record ChatMessage(string Role, string Content);
