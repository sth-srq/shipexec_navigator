# ShipExecAgent.BusinessLogic

Core business logic library that orchestrates all communication with the
ShipExec Management Studio REST API and drives the diff/apply pipeline.

---

## Contents

- [Responsibilities](#responsibilities)
- [Key Classes](#key-classes)
- [Diff / Apply Pipeline Detail](#diff--apply-pipeline-detail)
- [Entity Generators](#entity-generators)
- [Logging](#logging)

---

## Responsibilities

- **Authentication** — extracting Bearer tokens from JWT JSON via `JWTManager`
- **API communication** — synchronous HTTP POST calls to Management Studio endpoints
- **Company export** — assembling a multi-endpoint company XML document via `CompanyExportRequestGenerator`
- **Variance detection** — comparing two `Company` object graphs field-by-field across all entity types
- **Request generation** — converting variances into typed API request payloads
- **Apply** — executing generated requests against the live API and returning per-request results

---

## Key Classes

### `AppManager`
The single entry-point used by `ShipExecService` in the Blazor layer.

| Method | Description |
|---|---|
| `GetAccessToken()` | Extracts `access_token` from the stored JWT JSON |
| `GetCompanies()` | `POST /GetCompanies` — returns all companies for the current credential |
| `GetCompanyXmlString(...)` | Delegates to `CompanyExportRequestGenerator` to produce a full XML document |
| `GetShippers()` / `GetUsers()` / etc. | Individual entity fetches |
| `GetVariancesAndRequests(original, modified)` | Core diff method — deserialises XML → computes variances → builds requests |
| `ApplyChanges(modifiedXml, variances)` | Executes the generated requests and returns `ApplyChangeResult` items |

**Constructors:**
- `AppManager(string jwtString, string adminUrl)` — standard Blazor path (JWT already in memory)
- `AppManager(Guid companyGuid, string companyName, string jwtFilePath, string adminUrl)` — legacy file-based path

### `JWTManager`
Thin wrapper over `JsonHelper.Deserialize<JWT>` that extracts `access_token` from raw JWT JSON.

### `CompanyBuilderManager`
Aggregates all 14 per-entity generators.  Used inside `AppManager` to:
1. `GetVariances(existing, modified)` — collects variances from every generator + site children
2. `GetRequests(modified, variances)` — converts variances to HTTP request descriptors
3. `ApplyRequests(requests)` — POSTs each request and collects responses

### `CompanyExportRequestGenerator`
Calls multiple Management Studio endpoints in sequence (`GetCompany`, `GetCompanyProfiles`,
`PopulateShippers`, `PopulateClients`, …) to assemble a single `Company` XML file.

- **Full export:** `SaveCompanyToOutputFile(...)` — populates every sub-entity collection
- **Selective export:** `PopulateCompanySelective(ref company, sections)` — only fetches the listed sections (used for lazy tree loading)

### `CompanyExtractor`
Deserialises a company XML string or file into a typed `PSI.Sox.Company` object graph using `XmlSerializer`.

### `RequestGenerationBase<...>`
Generic base class for all per-entity generators.  Provides:
- `Get()` / `Get(id)` — fetch all or single entity
- `BaseAdd()` / `BaseUpdate()` / `BaseRemove()` — CRUD HTTP calls
- `GetVariances(current, modified)` — symmetric list diff producing `Variance` objects
- `GetRequests(modified, variances)` — maps variances to `RequestBaseWithURL` objects

Subclasses override `HasSameId` and `ShouldUpdate` to define identity and change detection for each entity type.

---

## Diff / Apply Pipeline Detail

```
GetVariancesAndRequests(originalXml, modifiedXml)
│
├─ CompanyExtractor.GetCompany(originalXml)  →  Company existingCompany
├─ CompanyExtractor.GetCompany(modifiedXml)  →  Company modifiedCompany
│
└─ CompanyBuilderManager.GetVariances(existing, modified)
        │
        ├─ ClientRequestGenerator.GetVariances(...)
        ├─ ShipperRequestGenerator.GetVariances(...)
        ├─ AdapterRegistrationRequestGenerator.GetVariances(...)
        ├─ CarrierRouteRequestGenerator.GetVariances(...)
        ├─ DataConfigurationMappingRequestGenerator.GetVariances(...)
        ├─ DocumentConfigurationRequestGenerator.GetVariances(...)
        ├─ MachineRequestGenerator.GetVariances(...)
        ├─ PrinterConfigurationRequestGenerator.GetVariances(...)
        ├─ PrinterDefinitionRequestGenerator.GetVariances(...)
        ├─ ProfileRequestGenerator.GetVariances(...)
        ├─ ScaleConfigurationRequestGenerator.GetVariances(...)
        ├─ ScheduleRequestGenerator.GetVariances(...)
        ├─ SourceConfigurationRequestGenerator.GetVariances(...)
        ├─ GetCompanyPropertyVariances(existing, modified)
        │
        └─ SiteRequestGenerator.GetVariances(...)
                └─ for each updated site:
                        ├─ MachineRequestGenerator (site machines)
                        ├─ ShipperRequestGenerator (site shippers)
                        ├─ ClientRequestGenerator (site clients)
                        ├─ PrinterConfigurationRequestGenerator (site printers)
                        ├─ PrinterDefinitionRequestGenerator
                        ├─ SourceConfigurationRequestGenerator
                        ├─ DataConfigurationMappingRequestGenerator
                        ├─ ScheduleRequestGenerator
                        └─ ProfileRequestGenerator

→ List<Variance>

CompanyBuilderManager.GetRequests(modified, variances)
→ List<RequestBaseWithURL>

CompanyBuilderManager.ApplyRequests(requests)
→ List<response objects>
```

---

## Entity Generators

| Generator | Entity | Endpoints |
|---|---|---|
| `ShipperRequestGenerator` | `Shipper` | GetShippers / AddShipper / UpdateShipper / RemoveShipper |
| `ClientRequestGenerator` | `Client` | GetClients / AddClient / UpdateClient / RemoveClient |
| `ProfileRequestGenerator` | `Profile` | GetCompanyProfiles / AddProfile / … |
| `SiteRequestGenerator` | `Site` | GetSites / AddSite / UpdateSite / RemoveSite |
| `AdapterRegistrationRequestGenerator` | `AdapterRegistration` | GetAdapterRegistrations / … |
| `CarrierRouteRequestGenerator` | `CarrierRoute` | GetCarrierRoutes / … |
| `DataConfigurationMappingRequestGenerator` | `DataConfigurationMapping` | GetDataConfigurationMappings / … |
| `DocumentConfigurationRequestGenerator` | `DocumentConfiguration` | … |
| `MachineRequestGenerator` | `Machine` | GetMachines / … |
| `PrinterConfigurationRequestGenerator` | `PrinterConfiguration` | … |
| `PrinterDefinitionRequestGenerator` | `PrinterDefinition` | … |
| `ScaleConfigurationRequestGenerator` | `ScaleConfiguration` | … |
| `ScheduleRequestGenerator` | `Schedule` | … |
| `SourceConfigurationRequestGenerator` | `SourceConfiguration` | … |

---

## Logging

Classes in this library are **not constructed through DI**.  They obtain loggers via the
static `LoggerProvider` gateway:

```csharp
private readonly ILogger<AppManager> _logger = LoggerProvider.CreateLogger<AppManager>();
```

`LoggerProvider.Initialize(factory)` is called once in `Program.cs` (Blazor startup)
before `app.Run()`.  Until then it returns `NullLogger` (no-op).

All methods follow the entry/exit trace convention:
```
_logger.LogTrace(">> MethodName | key={Value}", value);
// ... work ...
_logger.LogTrace("<< MethodName → result summary");
```
