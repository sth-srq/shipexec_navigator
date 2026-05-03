using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Embeddings;
using ShipExecAgent.RAGLoader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var docsFolder  = config["RAGLoader:DocumentsFolder"]
                  ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RAGDocuments");
var indexOutput = config["RAGLoader:IndexOutputPath"]
                  ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RAGDocuments", "rag_index.json");
var chunkSize    = int.Parse(config["RAGLoader:ChunkSize"]   ?? "500");
var chunkOverlap = int.Parse(config["RAGLoader:ChunkOverlap"] ?? "50");

// Resolve relative paths from the loader's base directory
docsFolder  = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, docsFolder));
indexOutput = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, indexOutput));

Console.WriteLine("ShipExecAgent RAG Document Loader");
Console.WriteLine("======================================");
Console.WriteLine($"Documents folder : {docsFolder}");
Console.WriteLine($"Index output     : {indexOutput}");
Console.WriteLine($"Chunk size       : {chunkSize} words  (overlap {chunkOverlap})");
Console.WriteLine();

if (!Directory.Exists(docsFolder))
{
    Console.WriteLine($"ERROR: Documents folder not found: {docsFolder}");
    return 1;
}

// --- Embedding service (local / hash-projection, 384-dim, no API calls) ---
var embeddingService = new LocalTextEmbeddingService();

// --- Discover files ---
var files = Directory.EnumerateFiles(docsFolder, "*.*", SearchOption.AllDirectories)
    .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
    .Where(f => !string.Equals(Path.GetFileName(f), "rag_index.json", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f)
    .ToList();

if (files.Count == 0)
{
    Console.WriteLine("No .txt or .pdf files found. Add documents to the RAGDocuments folder and re-run.");
    return 0;
}

Console.WriteLine($"Found {files.Count} file(s) to process.\n");

var allChunks = new List<RagChunk>();

foreach (var file in files)
{
    var relativeName = Path.GetRelativePath(docsFolder, file);
    Console.Write($"  [{relativeName}] extracting text ...");

    string text;
    try
    {
        text = ExtractText(file);
    }
    catch (Exception ex)
    {
        Console.WriteLine($" FAILED: {ex.Message}");
        continue;
    }

    var chunks = ChunkText(text, chunkSize, chunkOverlap);
    Console.Write($" {chunks.Count} chunk(s) ... embedding ");

    for (int i = 0; i < chunks.Count; i++)
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(chunks[i]);
        allChunks.Add(new RagChunk(
            Text:       chunks[i],
            Source:     relativeName,
            ChunkIndex: i,
            Embedding:  embedding.ToArray()));
        Console.Write(".");
    }

    Console.WriteLine(" done.");
}

Console.WriteLine();
Console.WriteLine($"Writing {allChunks.Count} chunk(s) to index ...");

var jsonOptions = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
};
var json = JsonSerializer.Serialize(allChunks, jsonOptions);
await File.WriteAllTextAsync(indexOutput, json, Encoding.UTF8);

Console.WriteLine($"Index written to: {indexOutput}");
Console.WriteLine("Done. Start ShipExecAgent — the AI will now use these documents for context.");
return 0;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static string ExtractText(string filePath)
{
    var ext = Path.GetExtension(filePath).ToLowerInvariant();

    if (ext == ".txt")
        return File.ReadAllText(filePath, Encoding.UTF8);

    if (ext == ".pdf")
    {

        using var doc = PdfDocument.Open(filePath, new ParsingOptions { UseLenientParsing = true });
        var sb = new StringBuilder();
        int pageNum = 0;

        foreach (var page in doc.GetPages())
        {
            pageNum++;
            var letters = page.Letters;

            // Split into letters with decoded Unicode values vs. unmapped glyphs
            var mappedLetters = letters.Where(l => !string.IsNullOrEmpty(l.Value)).ToList();
            var unmappedCount = letters.Count - mappedLetters.Count;

            string pageText = string.Empty;

            if (mappedLetters.Count > 0)
            {
                var extractedWords = NearestNeighbourWordExtractor.Instance
                    .GetWords(mappedLetters)
                    .ToList();

                pageText = extractedWords.Count > 0
                    ? string.Join(" ", extractedWords.Select(w => w.Text))
                    : string.Concat(mappedLetters.Select(l => l.Value));
            }

            // DEBUG — set a breakpoint on the next line to inspect extracted text before it is stored
            var debugPageText = pageText;

            var imgCount   = page.NumberOfImages;
            string diagnostic;
            if (letters.Count == 0 && imgCount > 0)
                diagnostic = $"image-only ({imgCount} image(s)) — PDF needs OCR, no text extracted";
            else if (letters.Count == 0)
                diagnostic = "no letters found — content may be inside Form XObjects or page is blank";
            else if (unmappedCount > 0)
                diagnostic = $"{letters.Count} letters ({unmappedCount} unmapped glyphs) → \"{Truncate(debugPageText, 100)}\"";
            else
                diagnostic = $"{letters.Count} letters → \"{Truncate(debugPageText, 100)}\"";

            Console.WriteLine($"      [p{pageNum}] {diagnostic}");

            if (!string.IsNullOrWhiteSpace(pageText))
                sb.AppendLine(pageText);
        }

        return sb.ToString();
    }

    throw new NotSupportedException($"Unsupported file type: {ext}");
}

static List<string> ChunkText(string text, int chunkSize, int overlap)
{
    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var chunks = new List<string>();
    int step = Math.Max(1, chunkSize - overlap);

    for (int i = 0; i < words.Length; i += step)
    {
        var chunk = string.Join(' ', words.Skip(i).Take(chunkSize));
        if (!string.IsNullOrWhiteSpace(chunk))
            chunks.Add(chunk);
        if (i + chunkSize >= words.Length)
            break;
    }

    return chunks;
}

// ---------------------------------------------------------------------------
// Model
// ---------------------------------------------------------------------------

static string Truncate(string s, int maxLength) =>
    s.Length > maxLength ? s[..maxLength] + "…" : s;
