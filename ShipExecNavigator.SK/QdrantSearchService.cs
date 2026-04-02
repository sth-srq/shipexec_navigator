using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using ShipExecNavigator.Shared.Interfaces;

namespace ShipExecNavigator.SK;

public sealed class QdrantSearchService : IVectorSearchService, IDisposable
{
    private readonly QdrantClient _qdrant;
    private readonly ITextEmbeddingGenerationService _embeddings;
    private readonly string _collection;
    private readonly ILogger<QdrantSearchService> _logger;

    public QdrantSearchService(IConfiguration configuration, ILogger<QdrantSearchService> logger)
    {
        _logger = logger;
        var endpoint         = configuration["AzureOpenAI:Endpoint"]           ?? string.Empty;
        var apiKey           = configuration["AzureOpenAI:ApiKey"]              ?? string.Empty;
        var embeddingModel   = configuration["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-ada-002";

        _embeddings = Kernel.CreateBuilder()
            .AddAzureOpenAITextEmbeddingGeneration(embeddingModel, endpoint, apiKey)
            .Build()
            .GetRequiredService<ITextEmbeddingGenerationService>();

        var host       = configuration["Qdrant:Host"]           ?? "localhost";
        var port       = int.Parse(configuration["Qdrant:Port"] ?? "6334");
        var qdrantKey  = configuration["Qdrant:ApiKey"];
        _collection    = configuration["Qdrant:CollectionName"] ?? "documents";

        _qdrant = string.IsNullOrEmpty(qdrantKey)
            ? new QdrantClient(host, port)
            : new QdrantClient(host, port, apiKey: qdrantKey);
    }

    public async Task<IReadOnlyList<string>> SearchAsync(
        string query, int topK = 5, CancellationToken ct = default)
    {
        _logger.LogTrace(">> SearchAsync(Qdrant) | Query={Query} TopK={TopK} Collection={Collection}",
            query.Length > 100 ? query[..100] : query, topK, _collection);
        try
        {
            var embedding = await _embeddings.GenerateEmbeddingAsync(query, cancellationToken: ct);

            var hits = await _qdrant.SearchAsync(
                _collection,
                embedding,
                limit: (ulong)topK,
                cancellationToken: ct);

            return hits
                .Select(h => h.Payload.TryGetValue("text", out var v) &&
                             v.KindCase == Value.KindOneofCase.StringValue
                             ? v.StringValue
                             : string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public void Dispose() => _qdrant.Dispose();
}
