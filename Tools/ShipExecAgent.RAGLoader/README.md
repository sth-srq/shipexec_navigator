# ShipExecAgent.RAGLoader

Offline console utility that converts ShipExec documentation (PDF and plain-text files)
into a local vector index consumed at runtime by `InMemoryRagService`.

Run this tool **once** whenever documentation changes.  It does **not** need to run
as part of the Blazor application startup.

---

## Contents

- [Overview](#overview)
- [Usage](#usage)
- [Configuration](#configuration)
- [Processing Pipeline](#processing-pipeline)
- [Output Format](#output-format)
- [Embedding Model](#embedding-model)
- [Updating the Index](#updating-the-index)

---

## Overview

```
RAGDocuments/
├── ShipExec_Guide.pdf
├── ConfigReference.txt
└── ...
          │
          ▼
   RAGLoader (console)
          │
          ▼
RAGDocuments/rag_index.json   ← consumed by InMemoryRagService at runtime
```

The tool:
1. Discovers all `.txt` and `.pdf` files in the configured documents folder.
2. Extracts plain text from each file (PDFPig for PDFs, direct read for text).
3. Splits the text into overlapping word-level chunks.
4. Generates a 384-dimensional embedding vector for each chunk using
   `LocalTextEmbeddingService` (no API calls required).
5. Writes all chunks + embeddings to a single `rag_index.json` file.

---

## Usage

```powershell
cd ShipExecAgent.RAGLoader
dotnet run
```

Console output:

```
ShipExecAgent RAG Document Loader
======================================
Documents folder : C:\...\RAGDocuments
Index output     : C:\...\RAGDocuments\rag_index.json
Chunk size       : 500 words  (overlap 50)

Found 3 file(s) to process.

  [ShipExec_Guide.pdf] extracting text ... 42 chunk(s) ... embedding .......... done.
  [ConfigReference.txt] extracting text ... 8 chunk(s) ... embedding ....... done.

Writing 50 chunk(s) to index ...
Done.  Index written to: C:\...\RAGDocuments\rag_index.json
```

---

## Configuration

Edit `ShipExecAgent.RAGLoader/appsettings.json`:

```json
{
  "RAGLoader": {
    "DocumentsFolder":  "../../../../RAGDocuments",
    "IndexOutputPath":  "../../../../RAGDocuments/rag_index.json",
    "ChunkSize":        "500",
    "ChunkOverlap":     "50"
  }
}
```

| Key | Default | Description |
|---|---|---|
| `DocumentsFolder` | `../../../../RAGDocuments` | Folder containing source documents |
| `IndexOutputPath` | `../../../../RAGDocuments/rag_index.json` | Where to write the output index |
| `ChunkSize` | `500` | Maximum words per chunk |
| `ChunkOverlap` | `50` | Words of overlap between consecutive chunks |

All paths are resolved relative to the tool's binary output directory.

---

## Processing Pipeline

```
For each file in DocumentsFolder:
│
├─ [.txt]  File.ReadAllText(file)
│
├─ [.pdf]  PdfDocument.Open(file)
│             ContentOrderTextExtractor.GetText(page, ...)  per page
│             Concatenate all pages
│
└─ ChunkText(text, chunkSize, chunkOverlap)
        Split into word tokens
        Sliding window of chunkSize words, step = chunkSize - chunkOverlap
   └─ For each chunk:
          LocalTextEmbeddingService.GenerateEmbeddingAsync(chunk)
          → float[384] embedding
          Append RagChunk(Text, Source, ChunkIndex, Embedding) to allChunks

Write allChunks to IndexOutputPath as JSON
```

---

## Output Format

`rag_index.json` contains a JSON array of `RagChunk` objects:

```json
[
  {
    "Text":       "The ShipExec shipper configuration defines...",
    "Source":     "ShipExec_Guide.pdf",
    "ChunkIndex": 0,
    "Embedding":  [0.0231, -0.1145, 0.0879, ...]
  },
  ...
]
```

The `Source` field is the relative path from `DocumentsFolder` for traceability.

---

## Embedding Model

`LocalTextEmbeddingService` uses the same `LocalEmbeddingGenerator` as the runtime
service — a deterministic hash-projection into 384 dimensions.

- **Advantage:** no API keys, no network calls, runs offline
- **Limitation:** keyword-proximity accuracy only (not semantic distance)

For production-quality RAG, configure a real embedding endpoint
(e.g. Azure OpenAI `text-embedding-ada-002`) in `LocalTextEmbeddingService` and
regenerate the index.

---

## Updating the Index

1. Add or update files in `RAGDocuments/`.
2. Re-run `dotnet run` — the entire index is rebuilt from scratch.
3. Copy or redeploy the updated `rag_index.json` alongside the Blazor application binary,
   or update `RAGLoader:IndexPath` in the Blazor `appsettings.json`.
