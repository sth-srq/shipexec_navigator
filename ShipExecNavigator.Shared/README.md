# ShipExecNavigator.Shared

Cross-cutting library referenced by every other project in the solution.
Contains interfaces, shared data models, helper utilities, and the logging gateway.

---

## Contents

- [Interfaces](#interfaces)
- [Models](#models)
- [Helpers](#helpers)
- [Logging Gateway](#logging-gateway)
- [Config](#config)

---

## Interfaces

Located in `Interfaces/`.  All service contracts are defined here so the Blazor layer
depends on abstractions, not concrete implementations.

| Interface | Purpose |
|---|---|
| `IShipExecService` | Primary façade — connect, explore, diff, apply, users, logs, templates |
| `IXmlRepository` | XML file I/O → `XmlNodeViewModel` tree |
| `IXmlViewerService` | Higher-level XML viewing (file/stream → tree + search) |
| `IXmlSchemaService` | Schema map for "Add child" context menu |
| `IXmlEnumService` | Enum-value dropdown data for XML editors |
| `IXmlRefLookupService` | Foreign-key reference lookup for dropdown editors |
| `IAiChatService` | AI chat completions (history + message → response) |
| `IVectorSearchService` | Semantic / vector search over the RAG index |

### `IShipExecService` — method groups

```csharp
// Authentication + company list
Task<List<CompanyInfo>> GetCompaniesAsync(string jwtJson, string adminUrl);

// Lazy XML tree
Task SetupCompanyAsync(Guid companyId, string companyName);
Task<XmlNodeViewModel> BuildCompanySkeletonAsync();
Task LoadCategoryChildrenAsync(XmlNodeViewModel categoryNode);

// Diff / apply
Task<string> GetCompanyXmlAsync(Guid companyId, string companyName, ...);
Task<DiffResult> GetDiffAsync(string originalXml, string modifiedXml);
Task<List<ApplyResultItem>> ApplyChangesAsync(string? comments = null);
Task<List<ApplyResultItem>> ApplyChangesAsync(IReadOnlyList<int> selectedIndices, string? comments);

// Users + permissions
Task<List<User>> GetUsersAsync();
Task<User?> GetUserDetailAsync(Guid userId);
Task<List<Permission>> GetPermissionsAsync(Guid userId);
Task UpdateUserPermissionsAsync(User user, List<Permission> permissions);
// ... roles, update, create, CSV, export, sites

// Logs
Task<(int Total, List<LogEntry> Logs)> GetApplicationLogsAsync(...);
Task<(int Total, List<SecurityLogEntry> Logs)> GetSecurityLogsAsync(...);

// Templates
Task<List<TemplateInfo>> GetCompanyTemplatesAsync(...);
Task StoreCompanyTemplatesAsync(...);
Task<bool> CompanyHasStoredTemplatesAsync(Guid companyId);
Task<List<TemplateSaveResult>> SaveTemplatesToFolderAsync(string folderPath);

// Shippers (variance-based import)
Task<List<PSI.Sox.Shipper>> GetShippersAsync();
Task<List<Variance>> GetShipperVariancesAsync(List<PSI.Sox.Shipper> incoming);
Task<List<ApplyResultItem>> ApplyShipperVariancesAsync(List<Variance> variances);
int AppendPendingVariances(List<Variance> variances);
```

---

## Models

| Model | Description |
|---|---|
| `XmlNodeViewModel` | Node in the rendered XML tree; holds children, attributes, diff state |
| `XmlAttributeViewModel` | Single XML attribute with original/current value tracking |
| `DiffResult` | Result of `GetDiffAsync`: list of `VarianceInfo` + `RequestInfo` |
| `VarianceInfo` | Display record for one variance (entity name, change type, before/after JSON) |
| `ApplyResultItem` | Per-entity apply outcome: entity name, operation, endpoint, success, message |
| `CompanyInfo` | Lightweight company record (Id, Name, Symbol) for the connect dialog |
| `UserInfo` | Display-oriented user summary |
| `LogEntry` / `SecurityLogEntry` | Management Studio log records |
| `CombinedLogEntry` | Merged app + security log for unified display |
| `TemplateInfo` | Template metadata from Management Studio |
| `TemplateSaveResult` | Outcome of saving a template to a local file |
| `ChildTemplate` / `ChildField` | Schema records for the "Add child" feature |
| `EnumOption` | Enum value + display label for dropdown editors |
| `RequestInfo` | Display record for one generated API request |
| `CsvUserRow` / `CsvUserCreateResult` | Bulk user import via CSV |

---

## Helpers

### `EntityTreeBuilder`

Converts PSI.Sox entity objects into `XmlNodeViewModel` trees using reflection,
matching the property names that `XmlSerializer` would produce.

| Method | Description |
|---|---|
| `FromObject(nodeName, obj, depth, parent)` | Builds a node tree from a single entity; scalars → leaf nodes, collections skipped |
| `PopulateCollectionNode(parentNode, itemName, items)` | Adds child nodes for every item in a collection; sets `IsLazyLoaded = true` |
| `CreateLazyCategoryNode(name, key, depth, parent)` | Creates an unexpanded placeholder node for a lazy-loadable category |

**Scalar types** recognized: `string`, `int`, `long`, `short`, `byte`, `float`, `double`,
`decimal`, `bool`, `Guid`, `DateTime`, `DateTimeOffset` (and their nullable variants).

**XmlSerializer name resolution:** `XmlArrayItemAttribute.ElementName` is preferred
over the CLR type name so node names match the actual XML element names.

### `CsvUserParser`

Parses a CSV file into a list of `CsvUserRow` objects for bulk user creation.
Validates required fields and returns parse errors in `CsvUserCreateResult`.

---

## Logging Gateway

```csharp
// Shared\Logging\AppLoggerFactory.cs
public static class AppLoggerFactory
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    public static void Initialize(ILoggerFactory factory) => _factory = factory;

    public static ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();
    public static ILogger CreateLogger(string categoryName) => _factory.CreateLogger(categoryName);
}
```

**Purpose:** non-DI classes (especially in `SK` and custom Blazor helpers) call
`AppLoggerFactory.CreateLogger<T>()` to get real loggers after DI is configured.

Initialised in `Program.cs`:
```csharp
AppLoggerFactory.Initialize(app.Services.GetRequiredService<ILoggerFactory>());
```

Returns `NullLogger` before initialisation (safe to call at any point).

---

## Config

### `NonEditableNodes`

Defines XML node paths where **inline editing is disabled**.  The restriction cascades
to all descendants.

```csharp
public static readonly HashSet<string> Paths = new(StringComparer.OrdinalIgnoreCase)
{
    "Profile.Shipper",
    "Profile.PaymentTerms",
    "Profile.Client",
};
```

**Path format:**
- `"Company.Profiles.Profile.Shipper"` — full path from root (exact)
- `"Profile.Shipper"` — suffix match (recommended; works regardless of root depth)

To add more non-editable paths, append to `Paths`.  
Context-menu Add/Remove operations are **not** affected — only inline value editing.

To discover a node's exact path, inspect the `data-nodepath` attribute on the node's
`<div>` in browser DevTools.
