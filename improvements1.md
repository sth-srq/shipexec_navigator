# ShipExec Navigator — Comprehensive Refactoring & Improvement Plan

## Context

You are working on a .NET 10 Blazor Server solution called **ShipExec Navigator** located at `C:\Users\Admin\Documents\GitHub\shipexec_navigator - Copy\`. The solution has 9 projects (+ 1 unloaded test project). It's an admin tool for a shipping platform (ShipExec) that connects to Management Studio APIs, displays company configurations as XML trees, supports diff/apply workflows, CSV user import/export, AI-assisted analysis (Azure OpenAI + Semantic Kernel), and blueprint-to-code generation.

**Branch:** `refactor`

**Projects:**
- `ShipExecNavigator.Blazor` (net10.0, Web/Blazor Server — the host app)
- `ShipExecNavigator.AppLogic` (net10.0, Library — 1 file only)
- `ShipExecNavigator.BusinessLogic` (net10.0, Library — PSI.Sox WCF interaction)
- `ShipExecNavigator.ClientSpecificLogic` (net10.0, Library — per-client strategy)
- `ShipExecNavigator.Model` (net10.0, Library — PSI.Sox domain types)
- `ShipExecNavigator.DAL` (net10.0, Library — Dapper + SQL)
- `ShipExecNavigator.SK` (net10.0, Library — Semantic Kernel AI)
- `ShipExecNavigator.Shared` (net10.0, Library — interfaces + models)
- `CodeStandards\TemplateCodeShipExec20BusinessRules` (.NET Framework 4.8 — SBR template)
- `ShipExecNavigator.Tests` (net10.0, xUnit — NOT in solution currently)

**Key constraint:** Do NOT modify `CodeStandards\TemplateCodeShipExec20BusinessRules\TemplateCodeShipExec20BusinessRules.csproj` beyond adding `<Compile>` entries. Do NOT add `PSI.Sox.Interfaces` to using statements. Follow all rules in `..\..\..\copilot-instructions.md`.

---

## CRITICAL Issues to Fix

### 1. Circular Dependency: Shared → BusinessLogic
`ShipExecNavigator.Shared.csproj` references `ShipExecNavigator.BusinessLogic.csproj`. This is backwards — Shared should be a leaf dependency. The cause is that `IShipExecService.cs` in Shared uses `ShipExecNavigator.BusinessLogic.EntityComparison.Variance` and other BL types.

**Fix:** Move `Variance`, `EntityComparison` namespace types from `BusinessLogic\ResponseModel\` into `ShipExecNavigator.Shared\Models\`. Remove the Shared → BusinessLogic project reference. Update all `using` statements.

### 2. God Object: `ShipExecService.cs` (2,128 lines, 40+ methods)
Split into focused services:
- `ConnectionService` — connect/disconnect, company selection, cached state, events
- `EntityIndexService` — `BuildEntityIndexAsync`, `BuildCompanySkeletonAsync`, `LoadCategoryChildrenAsync`
- `DiffApplyService` — `GetDiffAsync`, `ApplyChangesAsync`, variance history, `LogVariancesAsync`
- `UserManagementService` — all user CRUD, CSV parse/import/export, permissions, roles
- `ShipperService` — shipper export, import variances, merge logic
- `TemplateService` — template fetch/store/save
- `BusinessRuleService` — CBR/SBR get/save operations
- `LogQueryService` — application/security log queries

Each gets its own interface in Shared. `IShipExecService` becomes a thin facade or is eliminated entirely, with components injecting specific services.

### 3. God Object: `AppManager.cs` (1,201 lines)
Extract `IAppManager` interface. Split into Auth, DataRetrieval, VariancePipeline concerns. Inject via factory pattern since it needs runtime credentials.

### 4. `XmlViewer.razor` (4,767 lines)
Extract into partial class (`XmlViewer.razor.cs` for all C# code) and child components for logical sections (company header, variance bar, etc.).

### 5. `BlueprintAnalysisService.cs` (1,980 lines)
Split AI call logic, file I/O, and project templating into separate classes.

### 6. Tests Project Not in Solution
Add `ShipExecNavigator.Tests\ShipExecNavigator.Tests.csproj` to the `.sln` file. Add `<IsTestProject>true</IsTestProject>` to the test csproj.

### 7. Inconsistent PSI.Sox DLL Paths (4 different locations)
Consolidate all PSI.Sox DLLs into a single `lib/` folder at solution root. Update all `.csproj` references to point there.

---

## MODERATE Issues to Fix

### 8. No `Directory.Build.props`
Create at solution root:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```
Then remove duplicate properties from individual `.csproj` files. Exclude the .NET Framework 4.8 template project (use `Directory.Build.props` condition or place the template in a subdirectory without inheritance).

### 9. Add `.editorconfig`
Create at solution root with rules for:
- `csharp_style_namespace_declarations = file_scoped:warning`
- `dotnet_naming_rule` entries for `_camelCase` instance fields, PascalCase for public members
- `csharp_style_var_for_built_in_types = true:suggestion`
- Indent style: 4 spaces
- End of line: CRLF
- Insert final newline: true

### 10. Standardize File-Scoped Namespaces
Convert all block-scoped `namespace X { }` declarations to file-scoped `namespace X;` across all net10.0 projects. Files affected include: `Variance.cs`, `AppManager.cs`, `ClientLogicResolver.cs`, `IClientSpecificLogic.cs`, `DefaultCompanyLogic.cs`, `WesbancoClientSpecificLogic.cs`, `JWTManager.cs`, and all files in `BusinessLogic\`.

### 11. Remove Redundant `using` Statements
In `BusinessLogic` project files (which have `<ImplicitUsings>enable</ImplicitUsings>`), remove:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
```

### 12. Fix `OriginalXML`/`NewXML` Naming → `OriginalXml`/`NewXml`
Rename in: `VarianceInfo.cs`, `Variance.cs`, and all references throughout the solution. .NET convention: acronyms > 2 chars use PascalCase.

### 13. Extract `EnsureConnected()` Helper
In `ShipExecService.cs`, the pattern:
```csharp
if (_appManager is null)
    throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");
```
appears 30+ times. Replace with:
```csharp
private AppManager EnsureConnected() =>
    _appManager ?? throw new InvalidOperationException("Not connected. Call GetCompaniesAsync first.");
```

### 14. Replace `.ContinueWith()` with `async/await`
In `GetCompaniesAsync`, replace:
```csharp
return Task.Run(() => { ... }).ContinueWith(t => { _lastCompanies = t.Result; return t.Result; });
```
with proper async/await pattern.

### 15. Fix Nullable Warnings in `Variance.cs`
Add `= string.Empty` or mark as `string?` for all uninitialized string/object properties:
- `EntityName` → `string?` or add default
- `OriginalObject` → `object?`
- `NewObject` → `object?`
- `UndoAttributeName` → `string?`
- `SnapshotNode` → `object?`
- `Comments` → `string?`
- `VarianceJson` → `string?`

Remove `= false` from bool properties (redundant).

### 16. Fix `DateTime.Now` → `DateTime.UtcNow`
In `Variance.cs`: `public DateTime Timestamp { get; set; } = DateTime.UtcNow;`

### 17. Add `[JsonIgnore]` to `XmlNodeViewModel.Parent`
```csharp
[System.Text.Json.Serialization.JsonIgnore]
public XmlNodeViewModel? Parent { get; set; }
```

### 18. Fix CSP Policy — Remove `'unsafe-eval'`
In `Program.cs`, remove `'unsafe-eval'` from the Content-Security-Policy. Blazor Server does not need `eval`.

### 19. Add Health Check
```csharp
builder.Services.AddHealthChecks();
// ...
app.MapHealthChecks("/health");
```

### 20. Fix `AlertService` Event Race Condition
```csharp
public async Task ShowAlertAsync(string message)
{
    var handler = OnAlert;
    if (handler is not null)
        await handler.Invoke(message);
}
```

### 21. Add `.gitignore` Entries
Add:
```
**/Logs/
**/Properties/PublishProfiles/
*.pubxml
*.pubxml.user
```

### 22. Remove Committed Log File
Delete `ShipExecNavigator.Blazor\Logs\shipexec-navigator-json-20260407.json` from the repository.

### 23. Seal Model Classes
Add `sealed` to: `CompanyInfo`, `CbrInfo`, `SbrInfo`, `ApplyResultItem`, `RequestInfo`, `DiffResult`, `VarianceInfo`, `CompanyEntityIndex`, `XmlNodeViewModel`, `XmlAttributeViewModel`, `UserVariance`, `TemplateInfo`, `CsvUserRow`, `CsvUserCreateResult`.

### 24. Convert Pure DTOs to Records
Convert these classes to `record` types (they are created and never mutated):
- `CompanyInfo` → `record CompanyInfo`
- `ApplyResultItem` → `record ApplyResultItem`
- `RequestInfo` → `record RequestInfo`
- `CbrInfo` → `record CbrInfo`
- `SbrInfo` → `record SbrInfo`
- `DiffResult` → `record DiffResult`

### 25. Extract `AiChatRequest` Parameter Object
Replace the 10-parameter `IAiChatService.SendMessageAsync` with:
```csharp
public record AiChatRequest
{
    public required IReadOnlyList<ChatMessage> History { get; init; }
    public required string UserMessage { get; init; }
    public string? XmlContext { get; init; }
    public bool UseRag { get; init; } = true;
    public string? UsersContext { get; init; }
    public string? UserMetaContext { get; init; }
    public string? CbrsContext { get; init; }
    public string? LogsContext { get; init; }
    public CompanyEntityIndex? EntityIndex { get; init; }
}

Task<AiChatResponse> SendMessageAsync(AiChatRequest request, CancellationToken ct = default);
```

### 26. Define Entity Category Constants
```csharp
// ShipExecNavigator.Shared/Constants/EntityCategories.cs
public static class EntityCategories
{
    public const string Shippers = "Shippers";
    public const string Clients = "Clients";
    public const string Profiles = "Profiles";
    public const string Sites = "Sites";
    public const string Users = "Users";
    public const string AdapterRegistrations = "AdapterRegistrations";
    public const string CarrierRoutes = "CarrierRoutes";
    public const string ClientBusinessRules = "ClientBusinessRules";
    public const string DataConfigurationMappings = "DataConfigurationMappings";
    public const string DocumentConfigurations = "DocumentConfigurations";
    public const string Machines = "Machines";
    public const string PrinterConfigurations = "PrinterConfigurations";
    public const string PrinterDefinitions = "PrinterDefinitions";
    public const string ScaleConfigurations = "ScaleConfigurations";
    public const string Schedules = "Schedules";
    public const string ServerBusinessRules = "ServerBusinessRules";
    public const string SourceConfigurations = "SourceConfigurations";
}
```

### 27. Fix Indentation in `Program.cs`
Lines 34-35 are not indented (all other service registrations are indented with 4 spaces).

### 28. Remove `Debugger.Break()` Comments
Remove commented-out `//Debugger.Break()` and `//System.Diagnostics.Debugger.Break()` lines from `ShipExecService.cs`.

### 29. Make `ClientLogicResolver` Injectable
Convert from `static class` to a DI-registered service with an interface:
```csharp
public interface IClientLogicResolver
{
    IClientSpecificLogic Resolve(string? companyName);
}
```

### 30. Remove Static Logger Providers (Long-Term)
Replace `LoggerProvider.Initialize(loggerFactory)` pattern with DI-injected `ILogger<T>` in `AppManager` and `ClientLogicResolver`. This requires making `AppManager` accept `ILoggerFactory` in its constructor.

### 31. Add `CancellationToken` to `IShipExecService` Methods
All async methods should accept an optional `CancellationToken ct = default` parameter.

### 32. Fix `FieldDiffHelper` Performance
Replace:
```csharp
.Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase))
```
with a `HashSet<string>(StringComparer.OrdinalIgnoreCase)` lookup.

### 33. Merge `AppLogic` Project into Shared
`ShipExecNavigator.AppLogic` has only one file (`XmlViewerService.cs` — 227 lines). Move it into `ShipExecNavigator.Shared` (it implements `IXmlViewerService` which is already there) and remove the project.

### 34. Fix `EscapeCsv` — Quote Values with Leading/Trailing Whitespace
Per RFC 4180, values with leading/trailing spaces should be quoted:
```csharp
if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r') || value != value.Trim())
    return $"\"{value.Replace("\"", "\"\"")}\"";
```

### 35. Add `StringBuilder` Capacity Hint for CSV Export
```csharp
var sb = new StringBuilder(shippers.Count * 200);
```

### 36. Add Authorization to `/api/show-alert`
```csharp
app.MapPost("/api/show-alert", [...] ).RequireAuthorization();
```
Or at minimum add rate limiting.

### 37. Remove Full Bootstrap Distribution from wwwroot
Keep only `bootstrap.min.css` and `bootstrap.bundle.min.js`. Remove all source maps, RTL variants, unminified files, and ESM builds.

### 38. Move `check_ctor.csx` Out of Project Root
Move to a `scripts/` or `tools/` folder, or delete if no longer needed.

### 39. Namespace Consistency
Rename Blazor-project namespaces from:
- `ShipExecNavigator.Services` → `ShipExecNavigator.Blazor.Services`
- `ShipExecNavigator.Helpers` → `ShipExecNavigator.Blazor.Helpers`

### 40. Add `GlobalUsings.cs` Per Project
Example for Shared:
```csharp
global using ShipExecNavigator.Shared.Models;
```

---

## STYLE Rules to Apply Consistently

1. **File-scoped namespaces** everywhere (net10.0 projects only)
2. **PascalCase** for all public members; acronyms > 2 chars treated as words (`Xml`, not `XML`)
3. **`_camelCase`** for instance fields; no prefix for static readonly
4. **No redundant `= false`** on bool properties
5. **`sealed`** on all classes not designed for inheritance
6. **XML `<summary>` docs** on all public types and interface members
7. **No empty `catch` blocks** — at minimum log at Trace/Debug level
8. **`DateTime.UtcNow`** instead of `DateTime.Now` for all non-display timestamps
9. **Records** for immutable data-transfer objects
10. **Max ~400 lines per file** — extract when exceeding

---

## Execution Order (Recommended)

**Phase 0 — Repo Hygiene (30 min)**
- Add `.editorconfig`
- Add `.gitignore` entries (Logs/, PublishProfiles/)
- Delete committed log file
- Add `Directory.Build.props`
- Add Tests project to solution + `<IsTestProject>`
- Fix `Program.cs` indentation
- Remove `Debugger.Break()` comments

**Phase 1 — Break Circular Dependency (1-2 hrs)**
- Move `Variance` + `EntityComparison` types to Shared
- Remove Shared → BusinessLogic reference
- Update all using statements

**Phase 2 — Naming & Style Normalization (1-2 hrs)**
- File-scoped namespaces everywhere
- `OriginalXML` → `OriginalXml` rename
- Remove redundant usings
- Fix nullable warnings in `Variance.cs`
- `DateTime.UtcNow` fix
- Add `[JsonIgnore]` to Parent
- Seal model classes

**Phase 3 — Extract Helpers & Small Refactors (1-2 hrs)**
- `EnsureConnected()` helper
- Replace `.ContinueWith()` with async/await
- `AiChatRequest` parameter object
- `EntityCategories` constants
- Fix `AlertService` race condition
- Fix `EscapeCsv` and `FieldDiffHelper` performance
- Add `CancellationToken` parameters

**Phase 4 — Split God Objects (4-8 hrs)**
- Split `ShipExecService` into 7-8 focused services
- Split `IShipExecService` into focused interfaces
- Register new services in DI (`Program.cs`)
- Update all Blazor component `@inject` directives

**Phase 5 — Architecture Improvements (4-8 hrs)**
- Extract `IAppManager` interface + factory
- Make `ClientLogicResolver` injectable
- Merge `AppLogic` into Shared
- Eliminate static logger providers
- Add health check
- Consolidate PSI.Sox DLL paths
- Namespace rename (Blazor.Services/Helpers)

**Phase 6 — UI Cleanup (2-4 hrs)**
- Extract `XmlViewer.razor` into partial class + child components
- Remove unused Bootstrap files
- Add authorization to `/api/show-alert`
- Fix CSP policy

---

## Important Notes

- **Do NOT modify** `CodeStandards\TemplateCodeShipExec20BusinessRules\TemplateCodeShipExec20BusinessRules.csproj` (except adding `<Compile>` entries)
- **Do NOT add** `PSI.Sox.Interfaces` to any using statements
- **Do NOT change** business logic behavior — this is purely structural/style refactoring
- **Build and verify** after each phase
- The `.NET Framework 4.8` template project should be excluded from `Directory.Build.props` (use a condition or directory structure)
- When running tests, ignore differences in newlines and tab indentation per project conventions
