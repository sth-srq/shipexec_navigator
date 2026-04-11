using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ShipExecNavigator.Shared.Interfaces;

namespace ShipExecNavigator.SK.Plugins;

public sealed class RagSearchPlugin(IVectorSearchService vectorSearch, ILogger<RagSearchPlugin> logger)
{
    [KernelFunction("search_documents")]
    [Description("Search all documentation index for information relevant to the user's question. " +
                 "Call this whenever the user asks questions, instructions, installation steps, features, processes, documentation, requirements, configuration options, fields, or concepts.")]
    public async Task<string> SearchDocumentsAsync(
        [Description("The search query based on what the user is asking about.")] string query,
        CancellationToken cancellationToken = default)
    {
        logger.LogTrace(">> SearchDocumentsAsync | Query={Query}",
            query);
            //query.Length > 100 ? query[..100] : query);
        var chunks = await vectorSearch.SearchAsync(query, topK: 1000, cancellationToken);
        var result = chunks.Count == 0
            ? "No relevant documentation found for this query."
            : string.Join("\n---\n", chunks);
        logger.LogTrace("<< SearchDocumentsAsync → {ChunkCount} chunks", chunks.Count);
        return result;
    }
}
