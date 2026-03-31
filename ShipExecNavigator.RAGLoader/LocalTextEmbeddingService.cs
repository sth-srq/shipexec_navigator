namespace ShipExecNavigator.RAGLoader;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

/// <summary>
/// A fully local text-embedding service using term-frequency hash-projection.
/// No external API calls or additional NuGet packages required.
/// Produces 384-dimensional L2-normalised float vectors suitable for cosine similarity.
/// </summary>
internal sealed class LocalTextEmbeddingService : ITextEmbeddingGenerationService
{
    private const int Dimensions = 384;

    public IReadOnlyDictionary<string, object?> Attributes =>
        new Dictionary<string, object?> { ["dimensions"] = Dimensions };

    public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        IList<ReadOnlyMemory<float>> results = data.Select(Embed).ToList();
        return Task.FromResult(results);
    }

    private static ReadOnlyMemory<float> Embed(string text)
    {
        var vector = new float[Dimensions];
        var tokens = Tokenize(text);

        if (tokens.Count == 0)
            return vector;

        // Compute term frequency
        var tf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            tf.TryGetValue(token, out var count);
            tf[token] = count + 1;
        }

        // Project each term into two positions using independent hash functions.
        // Using (uint) cast avoids OverflowException on int.MinValue from Math.Abs.
        foreach (var (term, count) in tf)
        {
            float weight = MathF.Log(1f + count);

            int   h1 = (int)((uint)HashCode.Combine(term, 0) % (uint)Dimensions);
            int   h2 = (int)((uint)HashCode.Combine(term, 1) % (uint)Dimensions);
            float s1 = (HashCode.Combine(term, 2) & 1) == 0 ? 1f : -1f;
            float s2 = (HashCode.Combine(term, 3) & 1) == 0 ? 1f : -1f;

            vector[h1] += s1 * weight;
            vector[h2] += s2 * weight * 0.5f;
        }

        // L2 normalise so cosine similarity == dot product
        float magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0f)
        {
            for (int i = 0; i < Dimensions; i++)
                vector[i] /= magnitude;
        }

        return vector;
    }

    private static List<string> Tokenize(string text)
    {
        var tokens  = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (char c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                if (current.Length >= 2)         // ignore single-character noise
                    tokens.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length >= 2)
            tokens.Add(current.ToString());

        return tokens;
    }
}
