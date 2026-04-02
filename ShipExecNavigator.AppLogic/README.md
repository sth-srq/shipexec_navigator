# ShipExecNavigator.AppLogic

Provides the `XmlViewerService` — a higher-level XML viewing and manipulation service
used by the Blazor components that work directly with uploaded or pre-existing XML files
(as opposed to XML fetched live from the Management Studio API).

---

## Classes

### `XmlViewerService`

Implements `IXmlViewerService`.  Wraps `IXmlRepository` with additional
application-level operations:

| Method | Description |
|---|---|
| `LoadFromFileAsync(filePath)` | Opens an XML file from disk and returns the `XmlNodeViewModel` tree |
| `LoadFromStreamAsync(stream)` | Parses XML from an uploaded file stream |
| `SaveToFileAsync(root, filePath)` | Serialises the edited tree back to a UTF-8 XML file |
| `SearchAsync(root, query)` | Full-text search across all node names and values in the tree |

The service is registered as `Scoped` in the Blazor DI container so each circuit
(browser tab) maintains its own document state independently.

---

## Usage

```csharp
// Inject in a Razor component or page
@inject IXmlViewerService XmlViewer

// Load from an uploaded IBrowserFile
await using var stream = browserFile.OpenReadStream(maxAllowedSize: ...);
var root = await XmlViewer.LoadFromStreamAsync(stream);

// Load from disk (server path)
var root = await XmlViewer.LoadFromFileAsync("/data/company.xml");
```

---

## Relationship to XmlRepository

`XmlViewerService` delegates all serialisation / deserialisation to `IXmlRepository`
(implemented by `XmlRepository` in `ShipExecNavigator.DAL`).
`AppLogic` adds the search and higher-level orchestration layer on top.
