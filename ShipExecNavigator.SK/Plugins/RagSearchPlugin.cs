using System.ComponentModel;
using Microsoft.SemanticKernel;
using ShipExecNavigator.Shared.Interfaces;

namespace ShipExecNavigator.SK.Plugins;

/// <summary>
/// Semantic Kernel plugin that searches the local RAG document index.
/// Registered with the kernel when RAG is enabled so the LLM can call it
/// via function-calling whenever documentation context would help answer the user.
/// </summary>
public sealed class RagSearchPlugin(IVectorSearchService vectorSearch)
{
    [KernelFunction("search_documents")]
    [Description("Search the ShipExec documentation index for information relevant to the user's question. " +
                 "Call this whenever the user asks about ShipExec features, configuration options, fields, or concepts.")]
    public async Task<string> SearchDocumentsAsync(
        [Description("The search query based on what the user is asking about.")] string query,
        CancellationToken cancellationToken = default)
    {
        var chunks = await vectorSearch.SearchAsync(query, topK: 5, cancellationToken);
        return chunks.Count == 0
            ? "No relevant documentation found for this query."
            : string.Join("\n---\n", chunks);
    }
}
