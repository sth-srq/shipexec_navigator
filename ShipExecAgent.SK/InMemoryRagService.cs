using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShipExecAgent.Shared.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShipExecAgent.SK;

/// <summary>
/// In-process Retrieval-Augmented Generation (RAG) search service backed by a
/// pre-built JSON vector index produced by the <c>ShipExecAgent.RAGLoader</c> tool.
/// <para>
/// <b>How it works:</b>
/// <list type="number">
///   <item>
///     At startup, <see cref="InMemoryRagService"/> locates and loads the
///     <c>rag_index.json</c> file (a list of <see cref="RagChunk"/> objects each containing
///     a text excerpt and a pre-computed 384-dimensional embedding vector).
///   </item>
///   <item>
///     When <see cref="SearchAsync"/> is called, the query string is converted to an
///     embedding by <see cref="LocalEmbeddingGenerator.Embed"/> (a lightweight local
///     hash-projection — no network call required).
///   </item>
///   <item>
///     Cosine similarity is computed in a single LINQ pass over all chunks and the
///     top-<paramref name="topK"/> results are returned as plain text strings ready for
///     inclusion in an LLM system prompt.
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Index discovery order:</b>
/// <list type="number">
///   <item>Alongside the running binary (primary — populated by <c>CopyToOutputDirectory</c>).</item>
///   <item>Configured path relative to the binary directory.</item>
///   <item>Configured path relative to the current working directory.</item>
///   <item>Configured path as an absolute path.</item>
/// </list>
/// A warning is logged when no index file is found; the service returns empty results
/// rather than throwing, so the AI assistant degrades gracefully to "no docs found".
/// </para>
/// <para>
/// <b>Performance:</b> the entire index is held in memory after first load.
/// For typical ShipExec documentation corpora (a few thousand chunks) this is negligible.
/// For very large corpora, consider replacing this service with a
/// <see cref="QdrantSearchService"/> backed by a vector database.
/// </para>
/// </summary>
public sealed class InMemoryRagService : IVectorSearchService
{
    private readonly IReadOnlyList<RagChunk> _chunks;
    private readonly ILogger<InMemoryRagService> _logger;

    public InMemoryRagService(IConfiguration configuration, ILogger<InMemoryRagService> logger)
    {
        _logger = logger;
        var indexPath = ResolveIndexPath(configuration["RAGLoader:IndexPath"]);
        _chunks = LoadIndex(indexPath);
        _logger.LogInformation(_chunks.Count > 0
            ? "[RAG] Loaded {Count} chunks from {Path}"
            : "[RAG] No chunks loaded — index not found or empty at: {Path}",
            _chunks.Count, indexPath);
    }

    public Task<IReadOnlyList<string>> SearchAsync(
        string query, int topK = 5, CancellationToken ct = default)
    {
        _logger.LogTrace(">> SearchAsync | Query={Query} TopK={TopK} ChunkCount={Chunks}",
            query.Length > 100 ? query[..100] : query, topK, _chunks.Count);

        if (_chunks.Count == 0)
        {
            _logger.LogTrace("<< SearchAsync → 0 results (no chunks)");
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var queryVec = LocalEmbeddingGenerator.Embed(query);

        IReadOnlyList<string> results = _chunks
            .Select(c => (c.Text, Score: CosineSimilarity(queryVec, c.Embedding)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Text)
            .ToList();

        _logger.LogTrace("<< SearchAsync → {Count} results", results.Count);
        return Task.FromResult(results);
    }

    // Checks the binary output folder first (populated by CopyToOutputDirectory),
    // then falls back to the configured path resolved from several base directories.
    private static string ResolveIndexPath(string? configured)
    {
        var candidates = new List<string>();

        // 1. Alongside the running binary — the primary location after CopyToOutputDirectory
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "rag_index.json"));

        // 2. Configured path, tried relative to the binary directory and absolutely
        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configured)));
            candidates.Add(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configured)));
            candidates.Add(Path.GetFullPath(configured));
        }

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static IReadOnlyList<RagChunk> LoadIndex(string path)
    {
        if (!File.Exists(path))
            return [];

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<RagChunk>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;

        float dot = 0f, normA = 0f, normB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot   += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return denom == 0f ? 0f : dot / denom;
    }

    private sealed record RagChunk(
        string  Text,
        string  Source,
        int     ChunkIndex,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        float[] Embedding);
}
