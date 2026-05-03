# ShipExecAgent.SK

Semantic Kernel–based AI services providing chat completion, Retrieval-Augmented
Generation (RAG), and XML analysis plugins for the ShipExec Navigator AI Chat panel.

---

## Contents

- [Overview](#overview)
- [Classes](#classes)
- [Plugins](#plugins)
- [Configuration](#configuration)
- [RAG Index](#rag-index)
- [Replacing the Vector Store](#replacing-the-vector-store)

---

## Overview

This project provides two AI backends that implement `IAiChatService`:

| Class | Backend | When to use |
|---|---|---|
| `SemanticKernelChatService` | Azure OpenAI via Semantic Kernel | Default — supports RAG + XML plugins |
| `AiChatService` *(in Blazor)* | Raw OpenAI REST API | Fallback without SK |

`SemanticKernelChatService` is the default registration in `Program.cs`.

The Semantic Kernel kernel is constructed **per request** (not as a singleton) so
that a fresh plugin context is created for each user message.

---

## Classes

### `SemanticKernelChatService`

Implements `IAiChatService`.  On each `SendMessageAsync` call:

1. Reads `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:ChatDeployment`
   from configuration.
2. Returns a user-visible warning string when `ApiKey` is empty (graceful degradation).
3. Builds a `Kernel` with `AddAzureOpenAIChatCompletion`.
4. Imports `ShipperXmlPlugin` when `xmlContext` is non-empty.
5. Imports `RagSearchPlugin` when `useRag == true`.
6. Sets `FunctionChoiceBehavior.Auto()` so SK decides when to invoke plugins.
7. Converts the `history` list into a `ChatHistory` and appends the new user message.
8. Calls `GetChatMessageContentAsync` and returns the response string.

### `InMemoryRagService`

Implements `IVectorSearchService`.  Loaded once at startup.

**Index discovery order:**
1. `<AppContext.BaseDirectory>/rag_index.json` — primary (CopyToOutputDirectory)
2. Configured path relative to binary directory
3. Configured path relative to working directory
4. Configured path as absolute

**Search:** cosine similarity in a single LINQ pass over all loaded chunks.
Uses `LocalEmbeddingGenerator.Embed` for query vectorisation (no network call).

Returns top-K text strings for inclusion in the system prompt.

### `LocalEmbeddingGenerator`

Lightweight deterministic 384-dimensional embedding via hash projection.
- **No API calls** — entirely local / offline
- Consistent across runs (deterministic for a given model version)
- Suitable for keyword-proximity search; not as accurate as transformer embeddings

For higher-quality RAG, replace this with a real embedding model or use
`QdrantSearchService` with pre-computed transformer embeddings from the RAGLoader.

### `QdrantSearchService`

Optional alternative `IVectorSearchService` implementation backed by a
[Qdrant](https://qdrant.tech) vector database.  Not registered by default.
To enable: replace the `InMemoryRagService` registration in `Program.cs`.

### `RagChunk`

Record type representing one chunk in the vector index:

```csharp
record RagChunk(string Text, string Source, int ChunkIndex, float[] Embedding);
```

---

## Plugins

### `ShipperXmlPlugin`

Registered as `"ShipperXml"` on the kernel when XML context is provided.

**Function:** `find_shippers(currentHTML, userRequest)`  
Sends the loaded company XML and the user's request to the chat completion model
and returns a structured report of matching shippers plus DOM layout hints,
so the outer conversation can produce JavaScript that manipulates the Navigator tree.

### `RagSearchPlugin`

Registered as `"RagSearch"` on the kernel when RAG is enabled.

**Function:** `search_documents(query)`  
Calls `IVectorSearchService.SearchAsync` with the query and returns the top-5
matching documentation chunks as a newline-separated string for inclusion in the
AI response context.

---

## Configuration

```json
"AzureOpenAI": {
  "Endpoint":        "https://<your-resource>.openai.azure.com/",
  "ApiKey":          "<your-azure-openai-key>",
  "ChatDeployment":  "gpt-4o-mini"
},
"RAGLoader": {
  "IndexPath": "RAGDocuments/rag_index.json"
}
```

When `AzureOpenAI:ApiKey` is empty, `SemanticKernelChatService.SendMessageAsync`
returns a friendly warning string instead of throwing.

---

## RAG Index

The vector index is produced by `ShipExecAgent.RAGLoader`.  
Expected format: a JSON array of `RagChunk` objects:

```json
[
  {
    "Text": "...",
    "Source": "relative/path/to/doc.pdf",
    "ChunkIndex": 0,
    "Embedding": [0.123, -0.456, ...]
  },
  ...
]
```

The index is loaded once at startup and held in memory.  For a typical ShipExec
documentation corpus (a few hundred pages) this is a few MB.

---

## Replacing the Vector Store

To use a real vector database:

1. Register `QdrantSearchService` instead of `InMemoryRagService`:
   ```csharp
   builder.Services.AddSingleton<IVectorSearchService, QdrantSearchService>();
   ```
2. Configure the Qdrant endpoint and collection name in `appsettings.json`.
3. Re-run `RAGLoader` with transformer-quality embeddings (e.g. `text-embedding-ada-002`)
   and load the resulting index into Qdrant.
