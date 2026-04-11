using ShipExecNavigator.Shared.AI;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Shared.Interfaces;

public interface IAiChatService
{
    Task<AiChatResponse> SendMessageAsync(IReadOnlyList<ChatMessage> history, string userMessage, string? xmlContext = null, bool useRag = true, string? usersContext = null, string? userMetaContext = null, string? cbrsContext = null, string? logsContext = null, CompanyEntityIndex? entityIndex = null, CancellationToken ct = default);
}

public record ChatMessage(string Role, string Content, List<EntityNodeLink>? NodeLinks = null);
