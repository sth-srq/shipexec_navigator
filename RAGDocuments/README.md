# RAGDocuments

Place `.txt` and `.pdf` files in this folder (or sub-folders).

Run `ShipExecNavigator.RAGLoader` to process them:

```powershell
cd ShipExecNavigator.RAGLoader
dotnet run
```

The loader will:
1. Extract text from every `.txt` / `.pdf` file found here
2. Split each document into overlapping word chunks
3. Generate embeddings for each chunk via Azure OpenAI
4. Write `rag_index.json` to this folder

Once `rag_index.json` exists, the main ShipExecNavigator app automatically loads it into
memory at startup and uses it to answer AI chat questions with document context.

## Configuration

Chunk size and overlap are configurable in `ShipExecNavigator.RAGLoader/appsettings.json`:

| Key                        | Default | Meaning                              |
|----------------------------|---------|--------------------------------------|
| `RAGLoader:ChunkSize`      | 500     | Words per chunk                      |
| `RAGLoader:ChunkOverlap`   | 50      | Overlapping words between chunks     |
| `RAGLoader:DocumentsFolder`| (this folder) | Absolute or relative path to source files |
| `RAGLoader:IndexOutputPath`| `rag_index.json` in this folder | Where to write the index |
