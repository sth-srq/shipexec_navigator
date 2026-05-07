# ShipExec Navigator — Comprehensive Refactoring Instructions

You are refactoring the ShipExec Navigator solution. Below is a prioritized, exhaustive list of 58 improvements organized by category. Each item includes the problem, its impact, and the required fix. Apply these changes methodically, validating the build after each logical group.

## Solution Context

- **Solution root:** The workspace contains a Blazor Server app (`ShipExecNavigator.Blazor`) with supporting libraries targeting .NET 10.
- **Key external dependency:** PSI.Sox DLLs (proprietary ShipExec SDK) referenced across multiple projects.
- **AI integration:** Azure OpenAI via Microsoft Semantic Kernel (`ShipExecNavigator.SK`).
- **Database:** SQL Server accessed via Dapper (`ShipExecNavigator.DAL`).

---

## I. DEPENDENCY GRAPH PROBLEMS

### 1. Circular Dependency: `Shared ↔ BusinessLogic` [CRITICAL]
- **Problem:** `Shared` references `BusinessLogic` (via ProjectReference), but `Shared` is meant to be the lowest-level abstractions project. `IShipExecService` in `Shared` imports `ShipExecNavigator.BusinessLogic.EntityComparison.Variance`.
- **Impact:** Every consumer transitively pulls the entire stack; eliminates the value of separate assemblies.
- **Fix:** Move `Variance` class (and the `EntityComparison` namespace contents) from `BusinessLogic\ResponseModel\Variance.cs` into `Shared\Models\`. Move `ApplyChangeResult` and `GetResponseBase` similarly. Remove the `Shared → BusinessLogic` ProjectReference. Reverse the direction: `BusinessLogic` should reference `Shared`.

### 2. `IShipExecService` Is a God Interface (113 lines, 40+ methods) [HIGH]
- **Problem:** Single interface handles connection, XML, users, shippers, logs, profiles, templates, business rules, company creation, and variances.
- **Impact:** Violates Interface Segregation Principle. Testing requires mocking 40 methods. New developers can't reason about boundaries.
- **Fix:** Split into focused interfaces in `Shared\Interfaces\`:
  - `IShipExecConnectionService` — connect, disconnect, get companies, events
  - `IShipExecEntityService` — XML operations, diff, apply
  - `IShipExecUserService` — user CRUD, CSV, permissions
  - `IShipExecShipperService` — shipper CRUD, export, variances
  - `IShipExecLogService` — application/security logs
  - `IShipExecTemplateService` — templates, CBR, SBR
  - `IShipExecCompanyService` — create company, profiles

### 3. `ShipExecService` Is a 2,128-Line God Class [HIGH]
- **Problem:** Single class orchestrates all operations. Owns per-circuit state AND all business logic.
- **Impact:** Untestable, impossible to reason about state transitions, high regression risk.
- **Fix:** Decompose into:
  - `ShipExecConnectionManager` — connection state, JWT, company selection
  - `ShipExecEntityOrchestrator` — XML diffing, variance operations
  - `ShipExecUserOrchestrator` — user management
  - `ShipExecShipperOrchestrator` — shipper management
  - Each receives connection state via a shared `IConnectionContext` interface.

### 4. `AppManager` Is a 1,201-Line God Class [HIGH]
- **Problem:** Authentication + data retrieval + variance pipeline + HTTP orchestration all in one class.
- **Fix:** Split into:
  - `ShipExecAuthClient` — JWT handling, token refresh
  - `ShipExecApiClient` — HTTP operations, base URL management
  - `VariancePipeline` — deserialization → diff → request generation → apply

### 5. `Model` Project Contains Only 1 File (`JWT.cs`, 19 lines) [MEDIUM]
- **Problem:** A full project assembly for a single 4-property DTO.
- **Fix:** Move `JWT.cs` into `Shared\Models\`. Delete the `Model` project. Update all references.

### 6. `AppLogic` Project Contains Only 1 File (`XmlViewerService.cs`) [MEDIUM]
- **Problem:** Entire project for one service class (227 lines).
- **Fix:** Merge `XmlViewerService` into `Blazor\Services\` or into `BusinessLogic`. Delete the `AppLogic` project. Update all references.

### 7. `BusinessLogic` Mixes Concerns [MEDIUM]
- **Problem:** Contains API orchestration, XML conversion, request generation, DTOs, utilities, AND test fixtures.
- **Fix:**
  - Move `ResponseModel\Variance.cs` to `Shared\Models\`
  - Move `ResponseModel\ApplyChangeResult.cs` and `GetResponseBase.cs` to `Shared\Models\`
  - Move `Test\` folder contents to `ShipExecNavigator.Tests\TestData\`
  - Consider renaming project to `ShipExecNavigator.ApiClient`

### 8. `ClientSpecificLogic` Is Isolated from DI [MEDIUM]
- **Problem:** Defines `IClientSpecificLogic` internally, uses static `ClientLogicResolver.Resolve(companyName)` factory. Can't be mocked at the service level.
- **Fix:**
  - Move `IClientSpecificLogic` to `Shared\Interfaces\`
  - Add `Shared` as a project reference to `ClientSpecificLogic`
  - Register implementations in DI (keyed services or factory pattern)
  - Replace static resolver with an injected `IClientSpecificLogicFactory`

---

## II. CROSS-CUTTING CONCERNS

### 9. Three Separate Static `LoggerProvider` Classes [HIGH]
- **Problem:** `BusinessLogic.Logging.LoggerProvider`, `ClientSpecificLogic.Logging.LoggerProvider`, and `Shared.Logging.AppLoggerFactory` — all initialized in `Program.cs` with the same `ILoggerFactory`.
- **Impact:** Static mutable state, race-condition-prone, impossible to test with isolated loggers.
- **Fix:** Make `AppManager` and client-specific logic DI-resolvable. Use constructor-injected `ILogger<T>`. Eliminate all three static logger providers.

### 10. Inconsistent PSI.Sox.dll Reference Paths (5 Different Locations) [HIGH]
- **Problem:** Each project references PSI.Sox from a different path (absolute Program Files paths, relative `References\` paths, and even a path pointing to another repo).
- **Fix:** Standardize all references to a single `References\` folder at the solution root. Create `Directory.Build.props`:
  ```xml
  <Project>
    <PropertyGroup>
      <PsiSoxPath>$(MSBuildThisFileDirectory)References\</PsiSoxPath>
    </PropertyGroup>
  </Project>
  ```
  All projects use `$(PsiSoxPath)PSI.Sox.dll`.

### 11. Duplicate `Variance` Concepts Across Projects [MEDIUM]
- **Problem:** Three types named "Variance": `BusinessLogic.EntityComparison.Variance` (domain), `DAL.Entities.Variance` (DB), `Shared.Models.VarianceInfo` (UI).
- **Fix:** Rename for clarity:
  - DAL entity → `VarianceRecord`
  - BusinessLogic → move to `Shared` (since `IShipExecService` already depends on it)
  - Shared `VarianceInfo` → keep as-is (display model)

---

## III. SERVICE LAYER ANTI-PATTERNS

### 12. `Task.Run()` Wrapping Synchronous Code Everywhere [HIGH]
- **Problem:** Every method in `ShipExecService` does `return Task.Run(() => _appManager.SomeMethod())`. `AppManager` is entirely synchronous (uses HttpClient synchronously). This burns thread-pool threads.
- **Fix:** Make `AppManager` natively async (`HttpClient.SendAsync`). Until then, document the `Task.Run` as intentional for UI responsiveness.

### 13. `new AppManager(...)` Created Multiple Times for Side Requests [MEDIUM]
- **Problem:** Separate `AppManager` instances created for template/CBR fetches (lines 1683, 1720). Redundant JWT parsing, no HTTP client sharing.
- **Fix:** Extract an `IAppManagerFactory` that creates properly configured instances with shared HTTP infrastructure.

### 14. Swallowed Exceptions in Audit Logging [MEDIUM]
- **Problem:** `catch { /* Logging failures must never interrupt the apply flow */ }` — silent catch-all means audit trail gaps go undetected.
- **Fix:** At minimum `logger.LogError(ex, "Failed to persist variance audit")`. Consider a circuit-breaker or retry queue.

### 15. `ContinueWith` Instead of `await` [LOW]
- **Problem:** `}).ContinueWith(t => { ... })` — legacy TPL style, doesn't propagate exceptions properly.
- **Fix:** Replace with `await` + regular assignment.

### 16. Mutable Per-Circuit State Without Thread Safety [HIGH]
- **Problem:** `ShipExecService` holds mutable fields accessed from both circuit dispatcher and `Task.Run` (thread pool). Blazor guarantees single-threaded access within the dispatcher, but `Task.Run` escapes that.
- **Fix:** Either remove `Task.Run` (preferred) or protect shared state with `SemaphoreSlim`.

### 17. `BlueprintAnalysisService` is 1,980 Lines [MEDIUM]
- **Problem:** Another god class combining prompt building, AI HTTP calls, project file generation, and MSBuild validation.
- **Fix:** Split into:
  - `BlueprintPromptBuilder`
  - `BlueprintAiClient`
  - `BlueprintProjectGenerator`
  - `BlueprintValidationService`

### 18. Hardcoded AI Prompts in C# Code [MEDIUM]
- **Problem:** 50+ line string literals in `CbrAnalysisService`. Requires recompilation to tune prompts.
- **Fix:** Move system prompts to external files (`Prompts/` folder). Load at startup via `IWebHostEnvironment.ContentRootFileProvider`.

---

## IV. DATA MODEL & SERIALIZATION ISSUES

### 19. `XmlNodeViewModel.Parent` Creates Circular References [MEDIUM]
- **Problem:** Bidirectional tree links cause JSON serialization stack overflow without `ReferenceLoopHandling.Ignore`.
- **Fix:** Mark `Parent` with `[JsonIgnore]`. Use `Guid ParentId` for serialization scenarios. Reconstruct references after deserialization.

### 20. `DescendantCount` is O(n) Recursive Property Called During Render [MEDIUM]
- **Problem:** `public int DescendantCount => Children.Sum(c => 1 + c.DescendantCount);` walks entire subtree on every access.
- **Fix:** Cache the count, invalidate on structural changes.

### 21. `HasAnyChange` is O(n) Recursive [MEDIUM]
- **Problem:** Same as #20. If the tree has 10,000 nodes and UI calls this on root during re-render, it's O(n) per render.
- **Fix:** Dirty-propagation pattern: when a node changes, walk UP and set `_subtreeHasChange = true` on ancestors.

---

## V. AI/SK ARCHITECTURE ISSUES

### 22. `ShipperXmlPlugin` Parses XML Inside Every Function Call [LOW]
- **Problem:** Each kernel function independently calls `XDocument.Parse(_xmlContent)` — redundant for large XML.
- **Fix:** Parse once in constructor, store as field.

### 23. `IAiChatService.SendMessageAsync` Has 9 Parameters [MEDIUM]
- **Problem:** Every new context type changes the interface signature.
- **Fix:** Introduce a `ChatRequest` record:
  ```csharp
  public record ChatRequest(IReadOnlyList<ChatMessage> History, string UserMessage, ChatContext? Context = null, CancellationToken CancellationToken = default);
  public record ChatContext(string? XmlContext, bool UseRag = true, string? UsersContext, string? UserMetaContext, string? CbrsContext, string? LogsContext, CompanyEntityIndex? EntityIndex);
  ```

### 24. SK Plugins Call Back into the Kernel (Untestable) [MEDIUM]
- **Problem:** `ShipperXmlPlugin.FindShippers` calls `kernel.GetRequiredService<IChatCompletionService>()` — a plugin making AI calls. Can't unit test without full kernel setup.
- **Fix:** Plugins should be pure data-extraction functions. Move "send to AI for reasoning" back to `SemanticKernelChatService`.

---

## VI. DAL & PERSISTENCE ISSUES

### 25. No Repository Abstraction Over DAL Managers [MEDIUM]
- **Problem:** `VarianceManager`, `TemplateManager`, `ApiLogManager` are concrete classes injected directly.
- **Fix:** Add interfaces (`IVarianceRepository`, `ITemplateRepository`, `IApiLogRepository`) in `Shared\Interfaces\`.

### 26. No Database Migration Strategy [MEDIUM]
- **Problem:** SQL scripts are loose files with no ordering, no applied-tracking, no idempotency.
- **Fix:** Use DbUp or a numbered-script convention with an applied-migration tracking table.

### 27. Connection String Lifetime Mismatch [LOW]
- **Problem:** `IDbConnectionFactory` registered as Singleton with captured string. Won't respond to config reloads.
- **Fix:** Use `IOptions<T>` pattern or `IOptionsMonitor<T>`.

---

## VII. UI/BLAZOR COMPONENT ISSUES

### 28. No State Management Pattern [MEDIUM]
- **Problem:** Circuit state scattered across `ShipExecService`, `PendingImportService`, `AlertService`, and individual component `@code` blocks.
- **Fix:** Introduce a `CircuitState` / `NavigatorSession` class aggregating all per-circuit state with change notification.

### 29. `AlertService` is Singleton but Per-Circuit [MEDIUM]
- **Problem:** If two users trigger alerts simultaneously, they could see each other's messages.
- **Fix:** Make `AlertService` scoped (one per circuit).

### 30. 24 Dialog Components with No Shared Infrastructure [LOW]
- **Problem:** Every dialog reimplements show/hide, overlay, escape-key handling.
- **Fix:** Create a `DialogBase` component or `IDialogService` pattern for lifecycle, focus trapping, backdrop.

---

## VIII. SECURITY & OPERATIONAL CONCERNS

### 31. JWT Stored in Memory Without Expiry Check [MEDIUM]
- **Problem:** `_appManager` holds JWT indefinitely. No proactive refresh.
- **Fix:** Check expiry before API calls. Surface "session expired" UI. Implement refresh flow.

### 32. CSP Allows `unsafe-inline` and `unsafe-eval` [HIGH]
- **Problem:** Nullifies Content-Security-Policy XSS protection.
- **Fix:** Remove `unsafe-inline` and `unsafe-eval`. Use nonces or strict hashes only.

### 33. No Rate Limiting on AI Endpoints [MEDIUM]
- **Problem:** Rapid clicks on "Analyze" could exhaust Azure OpenAI quota.
- **Fix:** Client-side debouncing + server-side `SemaphoreSlim(1)` or rate-limiter middleware.

### 34. `Debugger.Break()` Left in Production Code [LOW]
- **Problem:** Commented-out `Debugger.Break()` calls throughout.
- **Fix:** Remove all instances. Use conditional breakpoints or `#if DEBUG` guards if needed.

---

## IX. BUILD & PROJECT HYGIENE

### 35. No `Directory.Build.props` [MEDIUM]
- **Problem:** No shared build properties. PSI.Sox paths, LangVersion, common settings all duplicated.
- **Fix:** Create `Directory.Build.props` at solution root with shared properties.

### 36. No `Directory.Packages.props` (Central Package Management) [MEDIUM]
- **Problem:** Package versions duplicated across projects (e.g., `Microsoft.Extensions.Logging.Abstractions 10.0.5` appears 4 times).
- **Fix:** Enable Central Package Management. Define all versions in `Directory.Packages.props`.

### 37. `<Folder Include="Shared\Interfaces\" />` in Blazor .csproj [LOW]
- **Problem:** Empty folder marker from earlier refactoring.
- **Fix:** Remove the ItemGroup entry and the empty folder.

### 38. `check_ctor.csx` in Blazor Project [LOW]
- **Problem:** Debugging script in production project.
- **Fix:** Move to `tools/` folder at solution root or delete.

### 39. `rag_index.json` and `Logs\` Committed to Source [LOW]
- **Problem:** Build artifacts and runtime output in source control.
- **Fix:** Add to `.gitignore`. Generate `rag_index.json` in CI/CD.

### 40. Dead `SkipGetTargetFrameworkProperties` Flags [LOW]
- **Problem:** Used when there was a framework mismatch. All projects now target net10.0.
- **Fix:** Audit and remove if no longer needed.

### 41. Test XML Files in `BusinessLogic\Test\` [LOW]
- **Problem:** Test fixtures in production code.
- **Fix:** Move to `ShipExecNavigator.Tests\TestData\`.

---

## X. TESTING GAPS

### 42. No Tests for `ShipExecService` (2,128 Lines) [HIGH]
- **Problem:** Critical logic untestable because `AppManager` is `new`-ed internally.
- **Fix:** After extracting `IAppManagerFactory`, write tests for `ApplyChangesAsync`, `GetDiffAsync`, `MergeShipper`, `GetShipperChangedFields`.

### 43. `ApplyUserEditFields` Needs Comprehensive Tests [MEDIUM]
- **Problem:** `internal static` method with 30+ field mappings. A typo silently breaks a field.
- **Fix:** Add parameterized `[Theory]` + `[InlineData]` tests for every field path.

### 44. No Snapshot Tests for XML Round-Tripping [HIGH]
- **Problem:** `XmlRepository` load → transform → save. Bugs silently corrupt company configurations.
- **Fix:** Add approval tests: load known XML, serialize back, assert equality.

### 45. No Integration Test Project [MEDIUM]
- **Problem:** One test project for everything. No separation between fast unit tests and slow integration tests.
- **Fix:** Add `ShipExecNavigator.Tests.Integration` for tests needing DB/network.

---

## XI. DOMAIN LOGIC IN WRONG LAYERS

### 46. Business Logic in Blazor Service Layer [HIGH]
- **Problem:** `MergeShipper`, `GetShipperChangedFields`, `BuildDiffResult`, `NormalizeForShipperDiff`, `StripUsersNode`, `EscapeCsv`, `BuildAddress`, `ApplyUserEditFields` — all domain logic in a UI service.
- **Fix:** Move to `BusinessLogic` or a new `ShipExecNavigator.Domain` project.

### 47. CSV Export Logic Not Abstracted [LOW]
- **Problem:** `ExportShippersCsvAsync` and `ExportUsersCsvAsync` both manually build CSVs with StringBuilder.
- **Fix:** Create a `CsvBuilder` utility class. Reduces 80-line methods to ~15 lines each.

### 48. `_canViewFlags` Hard-Coded Permission List [LOW]
- **Problem:** If a new permission is added to ShipExec, this array must be manually updated.
- **Fix:** Consider reflection-based discovery from `CsvUserRow` or externalize to configuration.

### 49. `EntityTreeBuilder` and `CsvUserParser` in Shared [LOW]
- **Problem:** These contain business logic (reflection-based conversion, CSV parsing) not shared infrastructure.
- **Fix:** Move to `BusinessLogic` or a `Domain` project.

---

## XII. DESIGN PATTERN ISSUES

### 50. `AppManager` Created via `new` Inside Scoped Service [HIGH]
- **Problem:** `AppManager` can't receive DI services, can't be mocked, can't be tested.
- **Fix:** Create `IAppManagerFactory`. Register factory in DI. Inject `ILogger<AppManager>` properly.

### 51. `Variance` Class Mixes Command + Query + Audit [MEDIUM]
- **Problem:** Domain properties + display properties + undo/snapshot + persistence tracking + temporal state all in one class. `object SnapshotNode` typed as object to "avoid cross-project dependency."
- **Fix:** Split into:
  - `ChangeIntent` (what to apply: entity, original, new, change type)
  - `ChangeDisplayItem` (UI: description, path, formatted values)
  - `ChangeAuditRecord` (persistence: JSON, comments, batch, timestamps)

### 52. No HTTP Abstraction in `BusinessLogic` [MEDIUM]
- **Problem:** `RequestGenerationBase` creates `HttpClient` with `new HttpClient()`. Socket exhaustion risk, can't mock, can't share handlers.
- **Fix:** Inject `IHttpClientFactory` or accept `HttpClient` in constructors.

### 53. Switch Statements on String Keys Instead of Polymorphism [MEDIUM]
- **Problem:** `LoadCategoryChildrenAsync` is a 100-line switch. Adding a category requires modifying the switch in TWO places (loading + indexing).
- **Fix:** Registry/strategy pattern:
  ```csharp
  Dictionary<string, ICategoryLoader> _loaders;
  ```

### 54. Redundant Code Between `BuildEntityIndexAsync` and `LoadCategoryChildrenAsync` [MEDIUM]
- **Problem:** Both fetch the same entities with the same fetchers but with different result handling. Copy-paste maintenance trap.
- **Fix:** Unify into a single `FetchCategory(key)` returning raw data. Each caller transforms independently.

---

## XIII. NAMING & CONVENTIONS

### 55. Namespace Inconsistencies [LOW]
- **Problem:** `ShipExecNavigator.Services` (Blazor, no `.Blazor` prefix), `ShipExecNavigator.Helpers` (Blazor), `BusinessLogic.EntityComparison` (lives in `ResponseModel\` folder).
- **Fix:** Align namespaces with project names. Move `Variance.cs` to an `EntityComparison\` folder or change namespace.

### 56. `CompanyInfo` Type Alias Required [LOW]
- **Problem:** `using CompanyInfo = ShipExecNavigator.Shared.Models.CompanyInfo;` needed because PSI.Sox has its own `CompanyInfo`.
- **Fix:** Rename to `NavigatorCompanyInfo` or `CompanySummary`.

### 57. `Wesbanco.cs` and `WesbancoClientSpecificLogic.cs` — Two Files? [LOW]
- **Problem:** Unclear distinction between the two files in `ClientSpecificLogic`.
- **Fix:** Consolidate or clarify.

---

## XIV. MISSING INFRASTRUCTURE

### 58. No Health Check Endpoints [MEDIUM]
- **Problem:** No `/health` or `/ready` endpoint for deployment orchestrators.
- **Fix:** Add `Microsoft.AspNetCore.Diagnostics.HealthChecks` with DB connectivity and Azure OpenAI reachability checks.

### 59. No Cancellation Token Propagation [MEDIUM]
- **Problem:** Most async methods don't accept `CancellationToken`. User navigating away mid-operation wastes resources.
- **Fix:** Thread `CancellationToken` through the service layer. Use `CancellationTokenSource` tied to component `Dispose`.

### 60. No Configuration/Options Pattern [MEDIUM]
- **Problem:** Configuration read inline (`builder.Configuration.GetConnectionString(...)`, `_configuration["AzureOpenAI:ApiKey"]`).
- **Fix:** Add strongly-typed options:
  - `ShipExecNavigatorOptions` (connection strings, feature flags)
  - `AzureOpenAiOptions` (endpoint, key, deployment)
  - Use `IOptions<T>` for testability and validation.

### 61. No Error/Result Pattern [MEDIUM]
- **Problem:** Methods return raw types or throw. No consistent fallible-operation pattern.
- **Fix:** Introduce `Result<T>` / `OperationResult<T>` in `Shared` for fallible operations.

### 62. No Structured Logging Middleware for API Calls [LOW]
- **Problem:** Manual `>> Method | ...` trace convention is easy to forget and inconsistent.
- **Fix:** Use delegating handlers on `HttpClient` for automatic request/response logging.

---

## Recommended Target Architecture

```
ShipExecNavigator.Shared          ← Interfaces, DTOs, models, options, Result<T> (LEAF - no project refs)
ShipExecNavigator.DAL             ← Data access (SQL + XML file I/O) → refs Shared
ShipExecNavigator.ApiClient       ← ShipExec HTTP API client (renamed BusinessLogic) → refs Shared, DAL
ShipExecNavigator.SK              ← AI/Semantic Kernel → refs Shared
ShipExecNavigator.ClientSpecific  ← Per-client overrides → refs Shared
ShipExecNavigator.Blazor          ← UI + DI composition root → refs all above
ShipExecNavigator.Tests           ← Unit tests → refs all above
ShipExecNavigator.Tests.Integration ← Integration tests (new)
CodeStandards\Template...         ← Unchanged (CBR/SBR template)
```

**Removed:** `Model` (merged into Shared), `AppLogic` (merged into Blazor/BusinessLogic)

---

## Execution Priority

| Priority | Items | Rationale |
|----------|-------|-----------|
| P0 (do first) | 1, 2, 3, 4, 9, 10, 46, 50 | Unblock testability and fix circular deps |
| P1 (high value) | 5, 6, 12, 16, 32, 35, 36, 42, 44 | Eliminate waste, fix security, enable testing |
| P2 (medium) | 7, 8, 11, 13, 17, 18, 23, 25, 28, 31, 33, 43, 51, 52, 53, 54, 58, 59, 60, 61 | Design improvements |
| P3 (cleanup) | 14, 15, 19, 20, 21, 22, 24, 26, 27, 29, 30, 34, 37, 38, 39, 40, 41, 45, 47, 48, 49, 55, 56, 57, 62 | Polish and hygiene |

---

## Constraints

- Do NOT add `PSI.Sox.Interfaces` to using statements — it does not exist.
- Do NOT modify `CodeStandards\TemplateCodeShipExec20BusinessRules\TemplateCodeShipExec20BusinessRules.csproj` except to add new `<Compile>` entries.
- Verify actual property types on PSI.Sox objects before assigning (e.g., `PackageExtras` is a `SerializableDictionary`, `Service` is a `Service` object).
- Preserve all PSI.Sox DLL references when restructuring projects.
- When testing, ignore differences in newlines and tab indentation.
