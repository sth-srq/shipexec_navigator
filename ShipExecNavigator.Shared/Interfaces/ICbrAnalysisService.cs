using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Shared.Interfaces;

public interface ICbrAnalysisService
{
    /// <summary>
    /// Sends <paramref name="cbrScript"/> and the CBRHelper.min.js library to the AI
    /// and returns refactored JavaScript that uses helper methods and includes
    /// comments highlighting the changes, with fixed variable naming and brace style.
    /// </summary>
    Task<string> AnalyzeCbrAsync(string cbrScript, CancellationToken ct = default);

    /// <summary>
    /// Sends a conversational chat message about <paramref name="cbrScript"/> to the AI,
    /// maintaining <paramref name="history"/> across turns.
    /// The response may include an optional revised script in <see cref="CbrChatResponse.Code"/>.
    /// </summary>
    Task<CbrChatResponse> ChatCbrAsync(
        IReadOnlyList<ChatMessage> history,
        string userMessage,
        string cbrScript,
        CancellationToken ct = default);
}
