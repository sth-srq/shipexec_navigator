# ShipExecNavigator.DAL

Data Access Layer — Dapper-based SQL Server persistence for audit records,
API call logs, XML snapshots, and company configuration templates.

---

## Contents

- [Overview](#overview)
- [Database Objects](#database-objects)
- [Classes](#classes)
- [Connection Factory](#connection-factory)
- [SQL Patterns](#sql-patterns)

---

## Overview

The DAL has two distinct responsibilities:

1. **SQL Server persistence** — reading and writing to `dbo.Variances`,
   `dbo.ApiLogs`, and `dbo.CompanyTemplates` via Dapper.

2. **XML file I/O** — `XmlRepository` parses ShipExec company XML (file or stream)
   into the `XmlNodeViewModel` tree that the Blazor UI renders, and serialises
   the tree back to formatted XML for saving.

All managers receive an `IDbConnectionFactory` via primary-constructor DI so
the connection string is never hard-coded and unit tests can supply an in-memory
connection.

---

## Database Objects

### `dbo.Variances`

Audit log of every entity change applied through Navigator.

| Column | Type | Description |
|---|---|---|
| `Id` | `bigint` | Auto-increment PK |
| `BatchId` | `uniqueidentifier` | Groups all changes from one "Apply" click |
| `CompanyId` | `uniqueidentifier` | Target company |
| `UserId` | `uniqueidentifier` | Actor (currently `Guid.Empty` — placeholder for future auth) |
| `NewEntity` | `nvarchar(max)` | JSON snapshot of the entity after the change |
| `OriginalEntity` | `nvarchar(max)` | JSON snapshot before the change |
| `VarianceData` | `nvarchar(max)` | Full serialised `Variance` object |
| `Comments` | `nvarchar(max)` | Optional comment entered by the user at apply time |
| `Endpoint` | `nvarchar(500)` | Admin API base URL the change was pushed to |
| `CreatedOn` | `datetime` | UTC timestamp (set by `VarianceManager.InsertAsync`) |
| `IsActive` | `bit` | Soft-delete flag |

### `dbo.ApiLogs`

Structured log of outbound API calls for operational monitoring.

| Column | Type | Description |
|---|---|---|
| `Id` | `bigint` | Auto-increment PK |
| `OccurredOn` | `datetime` | UTC timestamp |
| `Category` | `nvarchar(100)` | High-level grouping (e.g. "Company", "Shipper") |
| `Operation` | `nvarchar(200)` | Specific operation name |
| `RequestData` / `ResponseData` | `nvarchar(max)` | JSON payload snapshots |
| `DurationMs` | `bigint` | Round-trip time |
| `IsSuccess` | `bit` | HTTP 2xx indicator |
| `ErrorMessage` | `nvarchar(max)` | Set on failure |
| `CompanyId` | `uniqueidentifier` | Company in scope (nullable) |
| `AdditionalInfo` | `nvarchar(max)` | Free-form extra context |

### `dbo.CompanyTemplates`

Cached company configuration templates downloaded from the Management Studio.

| Column | Type | Description |
|---|---|---|
| `Id` | `bigint` | Auto-increment PK |
| `CompanyId` | `uniqueidentifier` | Owner company |
| `TemplateId` | `int` | Management Studio template ID |
| `CompanyName` | `nvarchar(200)` | Denormalised company name |
| `TemplateName` | `nvarchar(500)` | Display name |
| `TemplateType` | `nvarchar(100)` | Category / type |
| `TemplateData` | `nvarchar(max)` | Raw template payload |
| `EndpointUrl` | `nvarchar(500)` | Source API URL |
| `FetchedOn` | `datetime` | When the template was last downloaded |

---

## Classes

### `VarianceManager`
CRUD + soft-delete operations on `dbo.Variances`.

| Method | Description |
|---|---|
| `InsertAsync(variance)` | Inserts a new row; sets `CreatedOn` and `IsActive = true` |
| `GetByCompanyAsync(companyId)` | All variances for a company, newest first |
| `GetActiveByCompanyAsync(companyId)` | Only active (non-deactivated) rows |
| `DeactivateAsync(id)` | Sets `IsActive = 0` (soft delete) |
| `DeleteAsync(id)` | Hard delete |

### `ApiLogManager`
Write-heavy, read-occasionally log for API call monitoring.

| Method | Description |
|---|---|
| `InsertAsync(entry)` | Inserts one log row; sets `OccurredOn` |
| `GetRecentAsync(top)` | Most recent N rows across all companies |
| `GetByCompanyAsync(companyId, top)` | Most recent N rows for one company |

### `TemplateManager`
Upsert-based manager for template rows keyed on `(CompanyId, TemplateId)`.

| Method | Description |
|---|---|
| `UpsertAsync(template)` | `MERGE` on `(CompanyId, TemplateId)` |
| `UpsertBatchAsync(templates)` | Batch upsert inside a single transaction |
| `GetByCompanyAsync(companyId)` | All templates for a company, ordered by name |
| `HasTemplatesAsync(companyId)` | Quick existence check |

### `XmlRepository`
Converts between `XDocument` / file-system XML and the `XmlNodeViewModel` tree.

| Method | Description |
|---|---|
| `LoadFromFileAsync(filePath)` | Reads a file and builds the view-model tree |
| `LoadFromStreamAsync(stream)` | Same but from a `Stream` (e.g. uploaded file) |
| `SaveToFileAsync(root, filePath)` | Serialises the tree back to a UTF-8 XML file |
| `SerializeAsync(root)` | Returns the XML as a string (no file I/O) |

**Special ordering:** `Shippers` child elements are sorted by their `Id` attribute
descending so recently-created shippers appear at the top of the tree.

---

## Connection Factory

```csharp
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class SqlConnectionFactory : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}
```

Registered as `Singleton` in `Program.cs`.  Each manager opens and disposes
its own connection per operation (Dapper pattern — no shared ambient connection).

---

## SQL Patterns

- **Parameterised SQL** throughout — no string concatenation, no injection risk.
- **`SCOPE_IDENTITY()`** via `QuerySingleAsync<long>` to return the new PK after inserts.
- **`MERGE`** statements in `TemplateManager.UpsertAsync` handle insert-or-update atomically.
- **Raw string literals** (`"""..."""`) used for multi-line SQL for readability.
