using Microsoft.Extensions.Configuration;
using ShipExecNavigator.Shared.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShipExecNavigator.SK;

/// <summary>
/// Loads the RAG index produced by ShipExecNavigator.RAGLoader from a local JSON file
/// and answers similarity queries entirely in memory using the same local hash-projection
/// embeddings used to build the index — no external API or vector store required.
/// </summary>
public sealed class InMemoryRagService : IVectorSearchService
{
    private readonly IReadOnlyList<RagChunk> _chunks;

    public InMemoryRagService(IConfiguration configuration)
    {
        var indexPath = ResolveIndexPath(configuration["RAGLoader:IndexPath"]);
        _chunks = LoadIndex(indexPath);

        Console.WriteLine(_chunks.Count > 0
            ? $"[RAG] Loaded {_chunks.Count} chunks from {indexPath}"
            : $"[RAG] No chunks loaded — index not found or empty at: {indexPath}");
    }

    public Task<IReadOnlyList<string>> SearchAsync(
        string query, int topK = 5, CancellationToken ct = default)
    {
        if (_chunks.Count == 0)
            return Task.FromResult<IReadOnlyList<string>>([]);

        var queryVec = LocalEmbeddingGenerator.Embed(query);

        IReadOnlyList<string> results = _chunks
            .Select(c => (c.Text, Score: CosineSimilarity(queryVec, c.Embedding)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Text)
            .ToList();

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
