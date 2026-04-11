namespace ShipExecNavigator.SK;

/// <summary>
/// Fully local hash-projection text embedder (384-dim, no API calls).
/// Algorithm is intentionally identical to ShipExecNavigator.RAGLoader.LocalTextEmbeddingService
/// so that query vectors are comparable to the stored rag_index.json vectors.
/// </summary>
internal static class LocalEmbeddingGenerator
{
    private const int Dimensions = 384;

    public static float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        var tokens = Tokenize(text);

        if (tokens.Count == 0)
            return vector;

        var tf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
        {
            tf.TryGetValue(token, out var count);
            tf[token] = count + 1;
        }

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
                if (current.Length >= 2)
                    tokens.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length >= 2)
            tokens.Add(current.ToString());

        return tokens;
    }
}
