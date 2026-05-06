# ShipExec Navigator — AI Architecture Improvement Plan

## Context

You are working on the **ShipExec Navigator** solution — a Blazor Server application (.NET 10) that provides AI-powered management of ShipExec shipping software configuration. The solution uses Azure OpenAI via Microsoft Semantic Kernel for chat, Retrieval-Augmented Generation (RAG), and multi-pass code generation from business requirement "blueprint" documents.

The workspace is located at: `C:\Users\Admin\Documents\GitHub\shipexec_navigator - Copy\`  
Branch: `refactor`  
Repository: `https://github.com/sth-srq/shipexec_navigator`

---

## Solution Structure

| Project | Purpose |
|---------|---------|
| `ShipExecNavigator.Blazor` | Blazor Server UI, fallback `AiChatService`, `BlueprintAnalysisService`, `CbrAnalysisService` |
| `ShipExecNavigator.SK` | Semantic Kernel chat service, plugins (RAG, ShipperXml, EntityXml, UserXml, Logs, CompanyIndex) |
| `ShipExecNavigator.Shared` | Shared interfaces (`IAiChatService`), models, `NavigatorDomCheatSheet.cs` |
| `ShipExecNavigator.AppLogic` | Application logic |
| `ShipExecNavigator.BusinessLogic` | Business logic |
| `ShipExecNavigator.ClientSpecificLogic` | Client-specific logic |
| `ShipExecNavigator.DAL` | Data access layer |
| `ShipExecNavigator.Model` | Domain models |
| `ShipExecNavigator.Tests` | Unit tests |
| `CodeStandards\TemplateCodeShipExec20BusinessRules` | Template .csproj for generated SBR projects (.NET Framework 4.8) |

---

## Current AI Instruction Files

Located in `AIHelpers/`:

| File | Lines | Purpose |
|------|-------|---------|
| `shipexec-hooks.md` | 588 | Hook flows, decision guide, implementation rules |
| `shipexec-templates.md` | 1,102 | HTML template inventory + AngularJS directives |
| `PSI_Sox_Type_Reference.md` | 1,690 | Type docs with usage examples |
| `psi-sox-type-definitions.md` | 1,222 | Type docs with class definitions |

Additionally, prompts are embedded inline in:
- `ShipExecNavigator.SK\SemanticKernelChatService.cs` (~200 lines of system prompt via string concatenation)
- `ShipExecNavigator.Blazor\Services\AiChatService.cs` (~80 lines, duplicate of above)
- `ShipExecNavigator.Blazor\Services\CbrAnalysisService.cs` (~60 lines across const strings)
- `ShipExecNavigator.Blazor\Services\BlueprintAnalysisService.cs` (inline prompts for plan, report, repair steps)
- `ShipExecNavigator.Shared\AI\NavigatorDomCheatSheet.cs` (253-line markdown as C# const string)

Three files are referenced at runtime but **do not exist** (causing `FileNotFoundException`):
- `AIHelpers/sbr-system-prompt.md`
- `AIHelpers/cbr-system-prompt.md`
- `AIHelpers/template-system-prompt.md`

---

## Comprehensive Improvement Proposals

Implement the following changes in the order specified by the phases below. Each item has an ID for tracking.

---

### 🔴 Phase 1: Fix Crashes & Security (Do First)

#### #1 — Create Missing Prompt Files

Create these three files in `AIHelpers/`:

**`AIHelpers/sbr-system-prompt.md`** — Instructions for SBR (C# Server Business Rules) generation:
- Directive: "Generate complete C# SBR hook implementations"
- Expected JSON response format: `{ "sbrMethods": [{ "name": "...", "code": "..." }], "helperClasses": "..." }`
- Code style: namespace `ShipExec.<CompanyName>`, class `SoxBusinessRules : IBusinessObject`
- Rules: use `Logger` for all logging, use `BusinessRuleSettings` for config values, wrap DB/API calls in try/catch, check `ErrorCode == 0` in PostShip
- DO NOT: guess property types (consult psi-sox-types.md), add `PSI.Sox.Interfaces` to usings, hardcode connection strings

**`AIHelpers/cbr-system-prompt.md`** — Instructions for CBR (JavaScript Client Business Rules) generation:
- Directive: "Generate complete JavaScript CBR hook implementations"
- Expected JSON response format: `{ "cbrMethods": [{ "name": "...", "code": "..." }] }`
- Code style: constructor function pattern, camelCase variables, K&R braces
- Rules: never put DB/API code in CBR (use `thinClientAPIRequest` to call SBR `UserMethod`), use `client.alert.*` for messages
- Coordination: reference the SBR analysis output to ensure CBR and SBR hooks complement each other

**`AIHelpers/template-system-prompt.md`** — Instructions for HTML template generation:
- Directive: "Generate modified AngularJS HTML templates"
- Expected JSON response format: `{ "templateChanges": [{ "file": "...", "fullContent": "..." }], "cbrAdditions": "..." }`
- Rules: preserve existing template structure, only modify what the blueprint requires, use documented directives (`<field-select>`, `<name-address>`, etc.), maintain proper `ng-show`/`ng-hide` logic
- DO NOT: invent new custom directives, remove existing functionality, change keyboard shortcuts unless explicitly requested

#### #2 — Remove Stack Trace from Error Response

In `ShipExecNavigator.SK\SemanticKernelChatService.cs`, line ~329, change:
```csharp
return new AiChatResponse { Message = "⚠️ Azure OpenAI error: check server logs for details." + " _ " + ex.Message + " _ " + ex.StackTrace};
```
To:
```csharp
return new AiChatResponse { Message = "⚠️ Azure OpenAI error: check server logs for details." };
```
The exception is already logged on line 326.

---

### 🟠 Phase 2: Deduplicate Services & Prompts

#### #3 — Delete Fallback `AiChatService`

- Delete `ShipExecNavigator.Blazor\Services\AiChatService.cs`
- Remove its DI registration from `Program.cs` (if conditional) or ensure `SemanticKernelChatService` is the sole `IAiChatService` implementation
- If a "no-plugins" mode is needed, add a parameter to `SemanticKernelChatService.SendMessageAsync` to skip plugin imports
- Remove the `AiChat:*` configuration keys from `appsettings.json`

#### #4 — Extract Shared Azure OpenAI HTTP Client

Create `ShipExecNavigator.Shared\AI\AzureOpenAiClient.cs`:
```csharp
public interface IAzureOpenAiClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, bool jsonFormat, CancellationToken ct = default);
    Task<string> CompleteAsync(IReadOnlyList<object> messages, bool jsonFormat, CancellationToken ct = default);
}
```
- Implementation reads `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:ChatDeployment` from `IConfiguration`
- Uses `IHttpClientFactory` with Polly retry policy (see #19)
- Handles error responses, logging, and returns content string
- Register as singleton in DI
- Refactor `BlueprintAnalysisService` and `CbrAnalysisService` to consume this instead of their private HTTP methods

#### #5 — Extract Generic AI Repair Loop

Create `ShipExecNavigator.Shared\AI\AiRepairLoop.cs`:
```csharp
public static async Task RunRepairLoopAsync(
    IAzureOpenAiClient aiClient,
    string repairSystemPrompt,
    Func<Task<string>> validate,              // returns error string or empty
    Func<string, Task<string>> buildContext,  // errors → user message with affected files
    Func<string, Task> applyFixes,            // AI JSON response → write fixes to disk
    int maxAttempts,
    List<object> conversationHistory,
    Action<AiInteractionLog>? onInteraction,
    CancellationToken ct)
```
- Refactor the 3 repair loops in `BlueprintAnalysisService` (CBR, HTML, C# build) to use this
- Each loop provides its own validator, context builder, and fix applier as lambdas

#### #6 — Create Action Types Schema File

Create `AIHelpers/action-types-schema.md`:
```markdown
# Supported AI Action Types

These are the action types the AI can return in its JSON response to trigger operations in the Navigator UI.

| Type | Payload Schema | When to Use |
|------|---------------|-------------|
| `javascript` | `string` (JS code) | DOM manipulation only (hide/show/highlight elements) |
| `shipper-add` | `{ symbol, name, code, address1, city, stateProvince, postalCode, country, ... }` | Create a new shipper |
| `shipper-delete` | `[{ id, symbol, name }]` | Delete one or more shippers |
| `shipper-edit` | `[{ id, symbol, name, edits: { fieldName: newValue } }]` | Edit shipper fields |
| `user-find` | `[{ id, username, email }]` | Highlight/filter matching users |
| `user-add` | `{ email, company?, contact?, address1?, ... }` | Create a new user |
| `user-edit` | `[{ id, username, email, edits: { ... } }]` | Edit user fields |
| `user-delete` | `[{ id, username, email }]` | Delete users |
| `entity-delete` | `[{ entityType, id, name }]` | Delete any entity type |
| `entity-edit` | `[{ entityType, id, name, edits: { fieldName: newValue } }]` | Edit any entity type |
| `adapter-registration-delete` | `[{ id, name }]` | Delete adapter registrations |
| `adapter-registration-edit` | `[{ id, name, edits: { fieldName: newValue } }]` | Edit adapter registrations |
| `client-delete` | `[{ id, name }]` | Delete clients |
| `client-edit` | `[{ id, name, edits: { fieldName: newValue } }]` | Edit clients |
| `cbr-edit` | `{ id, name, script }` | Update a CBR script |
| `log-find` | `[{ id (int), source ("App" or "Security") }]` | Highlight matching log entries |

## Supported Edit Fields by Entity

### Shippers
Name, Symbol, Code, Address1, Address2, Address3, City, StateProvince, PostalCode, Country, Company, Contact, Phone, Fax, Email, Sms, PoBox, Residential

### Users — Top-level
Email, UserName, PhoneNumber, PasswordExpired (bool), LockoutEnabled (bool), EmailConfirmed (bool), PhoneNumberConfirmed (bool)

### Users — Address
Address.Company, Address.Contact, Address.Address1, Address.Address2, Address.Address3, Address.City, Address.StateProvince, Address.PostalCode, Address.Country, Address.Phone, Address.Fax, Address.Email, Address.Sms, Address.Account, Address.TaxId, Address.Code, Address.Group, Address.PoBox (bool), Address.Residential (bool)

### Users — Config
Config.ExportFileDelimiter (Comma/Semicolon/Tab), Config.ExportFileQualifier (None/DoubleQuotes/SingleQuote), Config.ExportFileGroupSeparator (Comma/Period), Config.ExportFileDecimalSeparator (Comma/Period)

### Users — Permissions & Roles
Permissions.Add (permission name), Permissions.Remove (permission name), Roles.Add (role name), Roles.Remove (role name)
```

Both `SemanticKernelChatService` and the copilot system prompt template should reference this file's content rather than embedding the schema inline.

#### #7 — Merge Type Reference Documents

- Merge `AIHelpers/psi-sox-type-definitions.md` and `AIHelpers/PSI_Sox_Type_Reference.md` into a single `AIHelpers/psi-sox-types.md`
- Organize by type: each type gets Definition → Properties Table → Key Relationships → SBR Usage (C#) → CBR Usage (JavaScript)
- Remove redundant content (both files define ShipmentRequest, Package, NameAddress, Service, etc.)
- Target: ~1,700 lines (down from ~2,900 combined)
- Delete the two original files after merging
- Update `BlueprintAnalysisService.cs` to reference the new single file path

#### #8 — Standardize Configuration Keys

- Remove `AiChat:ApiKey`, `AiChat:BaseUrl`, `AiChat:Model` from `appsettings.json` and `appsettings.Development.json`
- Ensure all AI services use only `AzureOpenAI:Endpoint`, `AzureOpenAI:ApiKey`, `AzureOpenAI:ChatDeployment`

---

### 🟡 Phase 3: Restructure Prompts

#### #9 — Extract Copilot System Prompt to File

Create `AIHelpers/copilot-system-prompt.md` containing the full system prompt currently built in `SemanticKernelChatService.cs` lines 107–230. Use template markers for conditional sections:

```markdown
# ShipExec Copilot — System Prompt

## Core Identity
You are ShipExec Copilot — an enthusiastic, knowledgeable assistant for ShipExec Navigator...

## Response Format
IMPORTANT: Always respond with a single valid JSON object — no markdown, no plain text, no code fences outside the JSON.
Use exactly this structure:
{ "message": "<your reply>", "action": { "type": "<type>", "payload": <payload> } }
Omit the "action" key entirely when no action is needed.

## Action Types
{{ACTION_TYPES_SCHEMA}}

## Markdown Formatting
The "message" value inside the JSON must use rich Markdown formatting...

## Interaction Style
- Be friendly and upbeat. Use an encouraging, positive tone.
- When ambiguous, ask a brief clarifying question before proceeding.
- After completing a task, suggest 1-2 logical next steps.

{{#if RAG}}
## Document Search
You have access to a document search tool. Call the search_documents function whenever the user asks about ShipExec features, configuration options, or concepts.
{{/if}}

{{#if ENTITY_INDEX}}
## Entity Index Mode
The user has a ShipExec company configuration loaded. A complete entity index is available.

**Entity manifest:**
{{MANIFEST_JSON}}

To get full field-level details for any category, call the `get_category_details` function.

### Shipper Operations
- FIND/SEARCH: call `find_shippers`
- DELETE/REMOVE: call `delete_shippers`, then respond with action type "shipper-delete"
- EDIT/UPDATE: call `edit_shippers`, then respond with action type "shipper-edit"
- ADD/CREATE: respond with action type "shipper-add" (no plugin call needed)

### Generic Entity Operations
(Profile, Site, CarrierRoute, ClientBusinessRule, DataConfigurationMapping, DocumentConfiguration, Machine, PrinterConfiguration, PrinterDefinition, ScaleConfiguration, Schedule, ServerBusinessRule, SourceConfiguration)
- FIND/INSPECT: call `find_entities` or `get_category_details`
- DELETE/REMOVE: call `delete_entities`, then respond with action type "entity-delete"
- EDIT/UPDATE: call `edit_entities`, then respond with action type "entity-edit"

Do NOT use action type "javascript" for entity operations.

### Profile Composition
Profiles are composed of other entities. When asked about profile relationships, look for BOTH child elements AND ID reference fields. Call `get_category_details` to resolve ID references.
{{/if}}

{{#if XML_LEGACY}}
## XML Legacy Mode
The user has a ShipExec XML configuration loaded in the Navigator.
(ShipperXml and EntityXml plugin instructions...)
{{/if}}

{{#if USERS}}
## User Operations
(find_users, edit_users, delete_users, user-add instructions...)
{{/if}}

{{#if LOGS}}
## Log Operations
(find_logs, log-find instructions...)
{{/if}}

{{#if CBRS}}
## CBR Operations
(cbr-edit instructions...)
Available CBRs: {{CBRS_CONTEXT}}
{{/if}}

{{#if DOM_REFERENCE}}
## Navigator DOM Reference
{{DOM_CHEAT_SHEET}}
{{/if}}
```

In `SemanticKernelChatService.cs`, replace the 200-line string concatenation with:
```csharp
var template = await File.ReadAllTextAsync(Path.Combine(aiHelpersPath, "copilot-system-prompt.md"), ct);
var systemPrompt = PromptTemplate.Render(template, new Dictionary<string, object?> {
    ["RAG"] = useRag,
    ["ENTITY_INDEX"] = hasIndex,
    ["XML_LEGACY"] = hasXml && !hasIndex,
    ["USERS"] = hasUsers,
    ["LOGS"] = hasLogs,
    ["CBRS"] = hasCbrs,
    ["DOM_REFERENCE"] = hasXml || hasIndex,
    ["MANIFEST_JSON"] = entityIndex?.ManifestJson ?? "",
    ["CBRS_CONTEXT"] = cbrsContext ?? "",
    ["ACTION_TYPES_SCHEMA"] = actionTypesSchema,
    ["DOM_CHEAT_SHEET"] = NavigatorDomCheatSheet.Content,
});
```

Create a simple `PromptTemplate` utility class in `ShipExecNavigator.Shared\AI\PromptTemplate.cs` that handles `{{#if VAR}}...{{/if}}` conditionals and `{{VAR}}` substitutions.

#### #10 — Split Hooks Document

Split `AIHelpers/shipexec-hooks.md` into:

**`AIHelpers/hooks-decision-guide.md`** (~90 lines) — Contains:
- The "Decision Guide: Which Hook to Use" table
- The "Implementation Rules" section (SBR Rules + CBR Rules)
- The "Response Format" section
- Always sent to every Blueprint Analysis pass

**`AIHelpers/hooks-flows.md`** (~500 lines) — Contains:
- All 14 detailed flow sequences (Load, Ship, Print, Void, Rate, Batch, CloseManifest, Transmit, AddressValidation, Groups, Reprocess, Pack, Page Events, Data Population)
- Sent selectively: after Step 1 (plan), only send the flows referenced in the plan's `sbrPlan` and `cbrPlan` arrays

Delete the original `shipexec-hooks.md` after splitting.

#### #11 — Move DOM Cheat Sheet to .md File

- Move content from `ShipExecNavigator.Shared\AI\NavigatorDomCheatSheet.cs` (the `const string`) to `AIHelpers/navigator-dom-reference.md`
- Change `NavigatorDomCheatSheet.cs` to load from file with lazy caching:
```csharp
public static class NavigatorDomCheatSheet
{
    private static string? _content;
    public static string Content => _content ??= LoadContent();
    private static string LoadContent()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "AIHelpers", "navigator-dom-reference.md");
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, "AIHelpers", "navigator-dom-reference.md");
        return File.Exists(path) ? File.ReadAllText(path) : "(DOM reference not found)";
    }
}
```

---

### 🟡 Phase 4: Token Optimization

#### #12 — Prune Per-Pass Context in Blueprint Analysis

In `BlueprintAnalysisService.cs`, change what each pass receives:

| Pass | Send | Don't Send |
|------|------|------------|
| Step 1 (Plan) | hooks-decision-guide + psi-sox-types (summary only) + templates (summary only) + blueprint | Full flows, full type details |
| Step 2 (SBR) | hooks-flows (only relevant flows from plan) + psi-sox-types + blueprint + plan | shipexec-templates.md |
| Step 3 (CBR) | hooks-flows (relevant) + psi-sox-types + blueprint + plan + SBR output | shipexec-templates.md |
| Step 4 (Templates) | shipexec-templates.md + blueprint + plan + CBR output | psi-sox-types, hooks-flows |
| Step 5 (Reports) | blueprint + plan | hooks, templates, types |

#### #13 — Conditional DOM Reference Injection

In `SemanticKernelChatService.cs`, only append `NavigatorDomCheatSheet.Content` when the user's message suggests UI manipulation. Simple heuristic:
```csharp
var uiKeywords = new[] { "hide", "show", "highlight", "scroll", "select", "click", "visible", "display", "color", "style", "dom", "element" };
var needsDom = uiKeywords.Any(k => userMessage.Contains(k, StringComparison.OrdinalIgnoreCase));
```
Or make it a plugin the AI can call on demand (preferred — lets the model decide).

#### #14 — Add Token Counting

- Add `Microsoft.ML.Tokenizers` package
- Before calling `GetChatMessageContentAsync`, estimate token count of the full prompt
- Log it: `_logger.LogInformation("Estimated tokens: {Tokens}", estimatedTokens)`
- If approaching limit (e.g., >120,000 for GPT-4o), truncate oldest history messages first
- Add a `MaxHistoryMessages` config value (default: 20) as a safety rail

---

### 🟡 Phase 5: Architecture Improvements

#### #15 — Decompose `BlueprintAnalysisService`

Split the 1,980-line class into:

- **`BlueprintOrchestrator`** — The public `AnalyzeAsync` method; coordinates the pipeline
- **`BlueprintPlanningService`** — Step 1 (plan generation + parsing)
- **`BlueprintCodeGenerator`** — Steps 2-5 (SBR/CBR/Template/Report AI calls + result parsing)
- **`BlueprintFileWriter`** — All file I/O: copy template project, write files, .csproj manipulation
- **`AiRepairService`** — Generic repair loop (from #5), used for CBR/HTML/C# validation

Each class should be independently unit-testable.

#### #16 — Prompt Composition Pattern

Replace the imperative if/else prompt building in `SemanticKernelChatService` with a declarative model:
```csharp
record PromptSection(string Name, Func<bool> IsActive, string Content);

var sections = new PromptSection[] {
    new("Core", () => true, corePrompt),
    new("RAG", () => useRag, ragPrompt),
    new("EntityIndex", () => hasIndex, entityIndexPrompt),
    new("XmlLegacy", () => hasXml && !hasIndex, xmlLegacyPrompt),
    new("Users", () => hasUsers, usersPrompt),
    new("Logs", () => hasLogs, logsPrompt),
    new("CBRs", () => hasCbrs, cbrsPrompt),
    new("DOM", () => needsDom, domPrompt),
};

var systemPrompt = string.Join("\n\n", sections.Where(s => s.IsActive()).Select(s => s.Content));
_logger.LogDebug("Active prompt sections: {Sections}", string.Join(", ", sections.Where(s => s.IsActive()).Select(s => s.Name)));
```

This makes it trivial to log which sections are active, test individual sections, and reorder them.

#### #17 — Remove Dead Iteration Loop

In `BlueprintAnalysisService.cs` (~line 175), remove the `for (var iteration = 0; iteration < 1; iteration++)` wrapper. Keep the body as straight-line code.

#### #18 — Cache Chat Completion Service

In `SemanticKernelChatService`, cache the `IChatCompletionService` instance (it's stateless — only configuration + HTTP client):
```csharp
private IChatCompletionService? _chatService;
private IChatCompletionService GetChatService()
{
    if (_chatService is not null) return _chatService;
    var kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey)
        .Build();
    _chatService = kernel.GetRequiredService<IChatCompletionService>();
    return _chatService;
}
```
Then build a fresh `Kernel` per-request only for plugin imports, passing the cached service.

---

### 🟢 Phase 6: Reliability

#### #19 — Add Polly Retry Policies

In `Program.cs`, configure the `IHttpClientFactory` with retry:
```csharp
builder.Services.AddHttpClient("AzureOpenAI")
    .AddPolicyHandler(Policy.Handle<HttpRequestException>()
        .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests || r.StatusCode == HttpStatusCode.ServiceUnavailable)
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));
```

All AI services should use this named client.

#### #20 — Update NuGet Packages

Update ALL NuGet packages to their latest stable versions. Priority packages:
- `Microsoft.SemanticKernel` and `Microsoft.SemanticKernel.Connectors.AzureOpenAI` — frequent releases with function-calling improvements, auto-retry, token management
- `QuestPDF` — check for latest stable
- All `Microsoft.Extensions.*` packages — align with .NET 10
- Test framework packages (`xunit`, `Moq`, `FluentAssertions`, etc.)
- Any `System.Text.Json` or `Microsoft.AspNetCore.*` packages

Run `dotnet list package --outdated` to identify all outdated packages, then update them.

---

### 🟢 Phase 7: Observability & Quality

#### #21 — Prompt Versioning

Add a version comment at the top of each prompt .md file:
```markdown
<!-- prompt-version: 1.0 | last-modified: 2025-07-XX | consumer: SemanticKernelChatService -->
```

In the AI client wrapper, compute and log a hash of the system prompt:
```csharp
var promptHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(systemPrompt)))[..8];
_logger.LogInformation("AI request | PromptHash={Hash} ...", promptHash);
```

#### #22 — Improve RAG Embeddings

Replace `LocalEmbeddingGenerator` (hash-projection) with Azure OpenAI `text-embedding-3-small`:
- Add a `UseAzureEmbeddings` config flag (default: `false` for backward compatibility)
- When enabled, call the embeddings endpoint for query vectorization
- Keep local embeddings as fallback when Azure is unavailable
- Consider enabling `QdrantSearchService` with pre-computed transformer embeddings for production

#### #23 — Create Prompt Manifest

Create `AIHelpers/MANIFEST.md`:
```markdown
# AI Instruction Files — Manifest

| File | Version | Consumer | Est. Tokens | Purpose |
|------|---------|----------|-------------|---------|
| copilot-system-prompt.md | 1.0 | SemanticKernelChatService | ~2,000 | Main chat copilot persona + instructions |
| action-types-schema.md | 1.0 | copilot-system-prompt (embedded) | ~500 | Action type definitions for Navigator UI |
| navigator-dom-reference.md | 1.0 | copilot-system-prompt (conditional) | ~1,000 | DOM/CSS selectors for JS actions |
| psi-sox-types.md | 1.0 | BlueprintAnalysisService | ~5,000 | PSI.Sox class/type reference |
| hooks-decision-guide.md | 1.0 | BlueprintAnalysisService (all passes) | ~400 | Which hook to use + implementation rules |
| hooks-flows.md | 1.0 | BlueprintAnalysisService (selective) | ~2,000 | Detailed hook execution sequences |
| shipexec-templates.md | 1.0 | BlueprintAnalysisService (template pass) | ~4,000 | HTML template inventory |
| sbr-system-prompt.md | 1.0 | BlueprintAnalysisService | ~200 | SBR generation directives |
| cbr-system-prompt.md | 1.0 | BlueprintAnalysisService | ~200 | CBR generation directives |
| template-system-prompt.md | 1.0 | BlueprintAnalysisService | ~200 | Template generation directives |

## Token Budget Guidelines
- Copilot chat (full context): ~4,000 tokens system prompt max
- Blueprint Analysis (per pass): ~8,000 tokens context max
- Total per blueprint run (5 passes): ~33,000 tokens target (down from ~50,000)
```

---

## Target File Structure After All Phases

```
AIHelpers/
├── MANIFEST.md                    # Registry + token budgets
├── copilot-system-prompt.md       # Templated copilot persona (from Phase 3, #9)
├── action-types-schema.md         # Action type definitions (from Phase 2, #6)
├── navigator-dom-reference.md     # DOM/CSS reference (from Phase 3, #11)
├── psi-sox-types.md               # Merged type reference (from Phase 2, #7)
├── hooks-decision-guide.md        # Compact decision table (from Phase 3, #10)
├── hooks-flows.md                 # Detailed flow sequences (from Phase 3, #10)
├── shipexec-templates.md          # HTML template inventory (kept, trimmed)
├── sbr-system-prompt.md           # SBR generation instructions (from Phase 1, #1)
├── cbr-system-prompt.md           # CBR generation instructions (from Phase 1, #1)
└── template-system-prompt.md      # Template generation instructions (from Phase 1, #1)
```

**Deleted after migration:**
- `psi-sox-type-definitions.md` → merged into `psi-sox-types.md`
- `PSI_Sox_Type_Reference.md` → merged into `psi-sox-types.md`
- `shipexec-hooks.md` → split into `hooks-decision-guide.md` + `hooks-flows.md`

---

## Key Rules When Implementing

1. **Never break existing functionality** — each phase should be independently deployable
2. **Preserve all existing prompt content** — restructure and deduplicate, don't lose instructions
3. **Test after each phase** — ensure `BlueprintAnalysisService`, `CbrAnalysisService`, and `SemanticKernelChatService` still work
4. **Follow the copilot-instructions.md rules** — don't add `PSI.Sox.Interfaces`, verify PSI.Sox property types, maintain all DLL references in template projects
5. **Keep the `TemplateCodeShipExec20BusinessRules.csproj` untouched** except for adding `<Compile>` entries
6. **Log everything** — every structural change should be observable in logs

---

## Expected Outcomes

| Metric | Before | After |
|--------|--------|-------|
| Tokens per blueprint run | ~50,000 | ~33,000 |
| Duplicated prompt lines across services | ~600 | 0 |
| Files containing AI prompts | 5 C# + 4 .md | 2 C# + 11 .md |
| Prompt edit → testable | Rebuild + redeploy | Edit .md + restart |
| Runtime crashes on fresh clone | 1 (BlueprintAnalysis) | 0 |
| `BlueprintAnalysisService` size | 1,980 lines | ~400 lines (orchestrator) |
| HTTP client implementations | 4 separate | 1 shared |
| Repair loop implementations | 3 separate | 1 generic |
