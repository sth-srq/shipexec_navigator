# ShipExec Navigator

A .NET 10 Blazor Server application for inspecting, editing, comparing, and applying
ShipExec Management Studio company configuration — with an integrated AI assistant
powered by Azure OpenAI and Retrieval-Augmented Generation (RAG).

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Solution Structure](#solution-structure)
4. [Control / Data Flow](#control--data-flow)
5. [Prerequisites](#prerequisites)
6. [Configuration](#configuration)
7. [Running the Application](#running-the-application)
8. [Building the RAG Index](#building-the-rag-index)
9. [Key Concepts](#key-concepts)
10. [Project READMEs](#project-readmes)
11. [Security Notes](#security-notes)

---

## Overview

ShipExec Navigator is an internal tooling application that lets operators:

| Capability | Description |
|---|---|
| **Connect** | Authenticate against the ShipExec Management Studio REST API using a JWT |
| **Explore** | Browse a full company configuration (shippers, clients, profiles, sites, etc.) as an interactive lazy-loading XML tree |
| **Edit** | Inline-edit node values and attributes directly in the browser |
| **Diff** | Compare the edited XML against the live snapshot to surface entity-level variances |
| **Apply** | Push selected variances back to ShipExec as typed Add / Update / Remove API requests |
| **Audit** | Persist every applied change to a SQL Server audit table (`dbo.Variances`) |
| **Templates** | Download and store company configuration templates locally |
| **Users** | Manage users, roles, permissions, and bulk-create via CSV |
| **Logs** | Browse application and security logs from the Management Studio |
| **AI Chat** | Ask natural-language questions about the loaded XML or ShipExec documentation |

---

## Architecture

```
Browser (Blazor Server — SignalR)
        │
        ▼
┌──────────────────────────────────────────────────────────┐
│  ShipExecNavigator.Blazor  (Razor Components + Services) │
│                                                          │
│  ┌────────────────────┐  ┌────────────────────────────┐  │
│  │  ShipExecService   │  │ SemanticKernelChatService  │  │
│  │  (IShipExecService)│  │ AiChatService (OpenAI)     │  │
│  └─────────┬──────────┘  └──────────┬─────────────────┘  │
└────────────┼──────────────────────┬─┘──────────────────┘
             │                      │
             ▼                      ▼
┌──────────────────────┐  ┌─────────────────────────────┐
│  BusinessLogic       │  │  ShipExecNavigator.SK       │
│  ├─ AppManager       │  │  ├─ InMemoryRagService       │
│  ├─ CompanyBuilder   │  │  ├─ LocalEmbeddingGenerator  │
│  │  Manager          │  │  ├─ QdrantSearchService      │
│  └─ RequestGen.*     │  │  └─ Plugins (RAG, ShipperXml)│
└──────────┬───────────┘  └─────────────────────────────┘
           │
           ▼
┌──────────────────────────────────────────────────────────┐
│  ShipExec Management Studio REST API  (PSI.Sox)          │
│  Endpoints: GetCompany, GetShippers, UpdateShipper, …    │
└──────────────────────────────────────────────────────────┘

┌──────────────────────┐   ┌──────────────────────────────┐
│  ShipExecNavigator   │   │  ShipExecNavigator.Shared    │
│  .DAL                │   │  (interfaces + models +      │
│  ├─ XmlRepository    │   │   helpers + logging gateway) │
│  ├─ VarianceManager  │   └──────────────────────────────┘
│  ├─ ApiLogManager    │
│  └─ TemplateManager  │
└──────────┬───────────┘
           ▼
     SQL Server
     dbo.Variances  · dbo.ApiLogs  · dbo.CompanyTemplates
```

---

## Solution Structure

| Project | Type | Purpose |
|---|---|---|
| [`ShipExecNavigator.Blazor`](ShipExecNavigator.Blazor/README.md) | Blazor Server | Web UI, DI root, all service registrations |
| [`ShipExecNavigator.BusinessLogic`](ShipExecNavigator.BusinessLogic/README.md) | Class Library | API orchestration, variance diff engine, request generation |
| [`ShipExecNavigator.DAL`](ShipExecNavigator.DAL/README.md) | Class Library | Dapper-based SQL Server data access |
| [`ShipExecNavigator.SK`](ShipExecNavigator.SK/README.md) | Class Library | Semantic Kernel AI, RAG search, SK plugins |
| [`ShipExecNavigator.RAGLoader`](ShipExecNavigator.RAGLoader/README.md) | Console App | Offline tool to build vector index from PDF/TXT docs |
| [`ShipExecNavigator.Shared`](ShipExecNavigator.Shared/README.md) | Class Library | Shared interfaces, models, helpers, logging gateways |
| [`ShipExecNavigator.AppLogic`](ShipExecNavigator.AppLogic/README.md) | Class Library | XML file-based viewer service |
| [`ShipExecNavigator.Model`](ShipExecNavigator.Model/README.md) | Class Library | JWT domain model |
| [`ShipExecNavigator.ClientSpecificLogic`](ShipExecNavigator.ClientSpecificLogic/README.md) | Class Library | Per-client logic overrides resolved by company name |
| `ShipExecNavigator.Tests` | xUnit Test | Unit and integration tests |

---

## Control / Data Flow

### 1 — Connect Flow

```
[Connect Dialog]
  User enters Admin URL + JWT JSON
    └─► ShipExecService.GetCompaniesAsync(jwtJson, adminUrl)
            └─► new AppManager(jwtJson, adminUrl)
            └─► AppManager.GetCompanies()   [POST /GetCompanies]
          ◄── List<CompanyInfo>
  User selects company
    └─► ShipExecService.SetupCompanyAsync(companyId, companyName)
    └─► ShipExecService.BuildCompanySkeletonAsync()
            └─► AppManager.GetCompanyBase() [POST /GetCompany]
          ◄── Shallow XmlNodeViewModel tree with lazy category nodes
```

### 2 — Lazy Tree Expansion

```
[User expands "Shippers" category node]
  └─► ShipExecService.LoadCategoryChildrenAsync(categoryNode)
          └─► switch(LazyLoadKey == "Shippers")
          └─► AppManager.GetShippers()      [POST /GetShippers]
          └─► EntityTreeBuilder.PopulateCollectionNode(...)
        ◄── categoryNode.Children filled; IsLazyLoaded = true
```

### 3 — Diff / Apply Flow

```
[User clicks "View Changes"]
  └─► ShipExecService.GetDiffAsync(originalXml, modifiedXml)
          └─► AppManager.GetVariancesAndRequests(original, modified)
                  └─► CompanyExtractor.GetCompany(xml)  ×2
                  └─► CompanyBuilderManager.GetVariances(existing, modified)
                          └─► per-entity generators (14 types + company props)
                          └─► site child entities (nested loop)
                  └─► CompanyBuilderManager.GetRequests(modified, variances)
        ◄── DiffResult { Variances, Requests }
  Phantom updates suppressed (no displayable scalar changes)

[User selects variances and clicks "Apply"]
  └─► ShipExecService.ApplyChangesAsync(selectedIndices, comments)
          └─► AppManager.ApplyChanges(modifiedXml, variances)
                  └─► CompanyBuilderManager.GetRequests(...)
                  └─► CompanyBuilderManager.ApplyRequests(requests)
                          └─► per-entity: POST Add/Update/Remove endpoint
        ◄── List<ApplyResultItem>
          └─► VarianceManager.InsertAsync(...)   [INSERT dbo.Variances]
```

### 4 — AI Chat Flow

```
[User sends message in AI Chat panel]
  └─► SemanticKernelChatService.SendMessageAsync(history, message, xmlContext, useRag)
          └─► Build Kernel (AzureOpenAI)
          └─► Import RagSearchPlugin    (if useRag == true)
          └─► Import ShipperXmlPlugin  (if xmlContext provided)
          └─► IChatCompletionService.GetChatMessageContentAsync(...)
                  SK auto-invokes search_documents / find_shippers as needed
        ◄── AI response string
```

---

## Prerequisites

- .NET 10 SDK
- SQL Server (any edition including LocalDB) — connection string required
- Azure OpenAI resource *(optional — AI Chat panel degrades gracefully when absent)*

---

## Configuration

Edit `ShipExecNavigator.Blazor/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ShipExecNavigator": "Server=.;Database=ShipExecNavigator;Trusted_Connection=True;"
  },
  "AzureOpenAI": {
    "Endpoint":        "https://<your-resource>.openai.azure.com/",
    "ApiKey":          "<your-key>",
    "ChatDeployment":  "gpt-4o-mini"
  },
  "RAGLoader": {
    "IndexPath": "RAGDocuments/rag_index.json"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default":   "Information",
      "Override": {
        "Microsoft": "Warning",
        "System":    "Warning"
      }
    },
    "WriteTo": [ { "Name": "Console" } ]
  }
}
```

> Use `dotnet user-secrets` for local dev — never commit secrets to source control.

---

## Running the Application

```powershell
cd ShipExecNavigator.Blazor
dotnet run
```

Navigate to `https://localhost:5001`.

---

## Building the RAG Index

1. Place ShipExec documentation (`.txt` or `.pdf`) into a `RAGDocuments/` folder.
2. Run the loader:
   ```powershell
   cd ShipExecNavigator.RAGLoader
   dotnet run
   ```
3. The tool writes `RAGDocuments/rag_index.json`.
4. Ensure the Blazor app can find the index via `RAGLoader:IndexPath` in `appsettings.json`
   or by setting `CopyToOutputDirectory` in the project file.

See [`ShipExecNavigator.RAGLoader/README.md`](ShipExecNavigator.RAGLoader/README.md) for full details.

---

## Key Concepts

### Variance
A `Variance` captures one entity-level difference between the original and modified
company XML:
- **Add** — entity present in modified but absent in original
- **Remove** — entity present in original but absent in modified
- **Update** — entity present in both; one or more scalar fields differ

Child variances (nested under a site) group site-child entity changes under their
parent site variance for cleaner display.

### Phantom Update Suppression
XML serialisation of complex/array properties can produce cosmetic differences that
do not represent real user edits.  `ShipExecService.IsPhantomUpdate` filters these
from the displayed diff — the underlying API requests are still generated correctly.

### Non-Editable Nodes
`NonEditableNodes` (`Shared.Config`) lists dot-separated node-path patterns where
inline editing is disabled (e.g. `"Profile.Shipper"`).  Restrictions cascade to all
descendant nodes.

### Logging
All services use structured Serilog.  Non-DI classes in `BusinessLogic` and
`ClientSpecificLogic` use static `LoggerProvider` gateways initialised once in
`Program.cs` before `app.Run()`.

---

## Project READMEs

- [Blazor](ShipExecNavigator.Blazor/README.md)
- [BusinessLogic](ShipExecNavigator.BusinessLogic/README.md)
- [DAL](ShipExecNavigator.DAL/README.md)
- [SK (Semantic Kernel)](ShipExecNavigator.SK/README.md)
- [RAGLoader](ShipExecNavigator.RAGLoader/README.md)
- [Shared](ShipExecNavigator.Shared/README.md)
- [AppLogic](ShipExecNavigator.AppLogic/README.md)
- [Model](ShipExecNavigator.Model/README.md)
- [ClientSpecificLogic](ShipExecNavigator.ClientSpecificLogic/README.md)

---

## Security Notes

- `DebugConfig.AutoConnect` **must be `false`** before committing.
- `DebugConfig.JwtToken` must **always remain empty** in committed code.
- JWT tokens and API keys must be stored in user-secrets or environment variables,
  never in `appsettings.json` in source control.
