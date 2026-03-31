namespace ShipExecNavigator.Shared.Interfaces;

public interface IVectorSearchService
{
    /// <summary>
    /// Embeds <paramref name="query"/> and returns the top-<paramref name="topK"/>
    /// matching text chunks from the vector store.
    /// </summary>
    Task<IReadOnlyList<string>> SearchAsync(string query, int topK = 5, CancellationToken ct = default);
}
