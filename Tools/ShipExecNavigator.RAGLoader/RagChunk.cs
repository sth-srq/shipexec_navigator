namespace ShipExecNavigator.RAGLoader;

internal sealed record RagChunk(
    string  Text,
    string  Source,
    int     ChunkIndex,
    float[] Embedding);
