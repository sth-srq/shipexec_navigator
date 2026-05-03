# ShipExecAgent.Blazor

The runnable Blazor Server web application.  This project is the **composition root** —
it hosts all Razor components, registers every service in the DI container, and
configures the ASP.NET Core pipeline.

---

## Contents

- [Responsibilities](#responsibilities)
- [Project Layout](#project-layout)
- [DI Registrations](#di-registrations)
- [Pipeline Configuration](#pipeline-configuration)
- [Services](#services)
- [DebugConfig](#debugconfig)

---

## Responsibilities

| Concern | Where |
|---|---|
| DI composition root | `Program.cs` |
| Razor component tree | `Components/` (App, layout, pages) |
| Service implementations | `Services/` |
| UI helpers | `Helpers/` |
| Debug-only flags | `DebugConfig.cs` |

---

## Project Layout

```
ShipExecAgent.Blazor/
├── Components/          Razor components (pages, dialogs, tree, AI chat, etc.)
│   └── App.razor        Root component / router
├── Services/
│   ├── ShipExecService.cs      Primary façade — implements IShipExecService
│   ├── XmlSchemaService.cs     Schema map for "Add child" context menu
│   ├── XmlEnumService.cs       Enum value lookup for dropdown editors
│   ├── XmlRefLookupService.cs  Foreign-key reference lookup for dropdowns
│   ├── AiChatService.cs        Fallback OpenAI chat (no Semantic Kernel)
│   └── AlertService.cs         Singleton event bus for UI alerts
├── Helpers/
│   └── FieldDiffHelper.cs      JSON-level field diff for variance detail view
├── DebugConfig.cs              Dev-only flags (AutoConnect, AdminUrl, JwtToken)
└── Program.cs                  ASP.NET Core host, DI, Serilog, pipeline
```

---

## DI Registrations

| Service | Lifetime | Interface / Type |
|---|---|---|
| `SqlConnectionFactory` | Singleton | `IDbConnectionFactory` |
| `VarianceManager` | Scoped | — |
| `TemplateManager` | Scoped | — |
| `ApiLogManager` | Scoped | — |
| `XmlRepository` | Scoped | `IXmlRepository` |
| `XmlViewerService` | Scoped | `IXmlViewerService` |
| `XmlEnumService` | Singleton | `IXmlEnumService` |
| `XmlSchemaService` | Singleton | `IXmlSchemaService` |
| `ShipExecService` | Scoped | `IShipExecService` |
| `XmlRefLookupService` | Scoped | `IXmlRefLookupService` |
| `AlertService` | Singleton | — |
| `InMemoryRagService` | Singleton | `IVectorSearchService` |
| `SemanticKernelChatService` | Scoped | `IAiChatService` |

> **Scoped** services get a fresh instance per SignalR circuit (browser tab).  
> **Singleton** services are shared across all circuits for the lifetime of the process.

---

## Pipeline Configuration

```csharp
// Logging
builder.Host.UseSerilog(...)           // structured Serilog
app.UseSerilogRequestLogging(...)      // per-request structured log enriched with host/scheme

// Error handling
app.UseExceptionHandler("/Error")      // production only
app.UseStatusCodePagesWithReExecute("/not-found")
app.UseHsts()                          // production only

// Static gateways (wired before app.Run)
AppLoggerFactory.Initialize(loggerFactory)
LoggerProvider.Initialize(loggerFactory)         // BusinessLogic
ClientSpecificLogic.LoggerProvider.Initialize(.) // ClientSpecificLogic

// Routing
app.UseAntiforgery()
app.MapStaticAssets()
app.MapRazorComponents<App>().AddInteractiveServerRenderMode()
app.MapPost("/api/show-alert", ...)    // external alert endpoint
```

---

## Services

### ShipExecService
The central Blazor-layer façade.  Owns the `AppManager` instance for the current
circuit and exposes async wrappers for every UI-visible operation.
See [`ShipExecAgent.BusinessLogic/README.md`](../ShipExecAgent.BusinessLogic/README.md)
for the underlying mechanics.

### XmlSchemaService
Singleton built once at startup via reflection over the PSI.Sox assemblies.  Maps
every `List<T>` property to the child element name and default field list so the
"Add child" context menu can insert a correctly-shaped blank node.

### XmlEnumService
Reads `[XmlEnum]` attributes from PSI.Sox types and caches a lookup of
`element-name → List<EnumOption>` used to render dropdown editors for enum fields.

### XmlRefLookupService
Scoped service that calls the live API to build dropdown options for reference fields
(e.g. Shipper ID lists within a Profile).

### AiChatService
Fallback implementation of `IAiChatService` that calls the raw OpenAI chat completions
endpoint without Semantic Kernel.  Used when the SK package is unavailable or when
`AiChat:ApiKey` is configured instead of `AzureOpenAI:ApiKey`.

### AlertService
Singleton event bus.  Components subscribe to `OnAlert`; the `/api/show-alert` endpoint
triggers it so external callers can push notifications into the live UI.

---

## DebugConfig

```csharp
internal static class DebugConfig
{
    public const bool AutoConnect = false;        // ⚠ NEVER commit as true
    internal const string AdminUrl  = "https://...";
    internal const string JwtToken  = "";         // ⚠ NEVER commit a token here
    internal const string AutoSelectCompany = "WebTest";
}
```

When `AutoConnect = true` the app auto-opens the connect dialog and selects the company
matching `AutoSelectCompany` — useful for rapid local iteration.  
**Must be `false` in all committed code.**
