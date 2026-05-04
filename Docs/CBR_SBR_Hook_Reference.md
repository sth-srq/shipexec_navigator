# ShipExec Business Rules – Hook Reference

> **Purpose:** A complete, prompt-ready reference for every lifecycle hook in both
> Client Business Rules (CBR – JavaScript, browser-side) and Server Business Rules
> (SBR – compiled .NET DLL, server-side).  Use this document as system-prompt context
> when generating, reviewing, or analysing business-rule scripts.

---

## Architecture Overview

| Aspect | CBR (Client Business Rules) | SBR (Server Business Rules) |
|---|---|---|
| **Language** | JavaScript (runs in browser) | .NET (compiled DLL, runs on server) |
| **Entry point** | `function ClientBusinessRules()` constructor | .NET class implementing SBR interface |
| **Scope** | Per-profile; one JS file per profile | Per-profile; one DLL per rule; linked via `ServerBusinessRuleId` on Profile |
| **Execution** | ShipExec Thin Client (Angular SPA) calls hooks at specific UI/workflow points | ShipExec server pipeline invokes hooks during request processing |
| **DOM access** | Yes – full jQuery / Angular `$scope` access | No – server-side only, no UI |
| **Available APIs** | `Tools.*`, `$.ajax`, Angular `$scope` / `vm`, DOM, `localStorage` | ShipExec WCF/REST service layer, database access |
| **Deployment** | Script text stored in `ClientBusinessRule.Script` field | Binary stored in `ServerBusinessRule.FileBytes` field |

---

## CBR Hooks (Client Business Rules)

All hooks are assigned as properties of the `ClientBusinessRules` constructor function.
The ShipExec Thin Client calls each hook at the corresponding point in the shipping workflow.

### Page & Navigation

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PageLoaded** | `function(location)` | Every SPA route change (`#!/shipping`, `#!/history`, `#!/batchdetail`, etc.) | Page-specific init, DOM manipulation, shipper sorting, address-code binding, hiding/showing UI elements |
| **Keystroke** | `function(shipmentRequest, event)` | Every keypress on the shipping form | Tab-key address lookups, barcode scan detection, field-level shortcuts |

### Shipment Lifecycle

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **NewShipment** | `function(shipmentRequest)` | A new (blank) shipment is created | Focus load input, copy origin to return address, set default cost center, bind address-code Tab search |
| **PreBuildShipment** | `function(shipmentRequest)` | Before the shipment request object is assembled from form data | Pre-populate fields, inject defaults before assembly |
| **PostBuildShipment** | `function(shipmentRequest)` | After the shipment request object is assembled | Validate assembled data, modify computed fields |
| **RepeatShipment** | `function(currentShipment)` | A shipment is duplicated / repeated | Clear or carry forward specific references |

### Ship (Process Shipment)

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreShip** | `function(shipmentRequest, userParams)` | Immediately before a shipment is submitted to the carrier | Timestamp stamping, dimension/weight validation, sync notification email from DOM, reference composition, `userParams` can cancel the shipment |
| **PostShip** | `function(shipmentRequest, shipmentResponse)` | After carrier returns a response | Display tracking info, trigger delegate labels, auto-print, file I/O |

### Ship Order

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreShipOrder** | `function(value, shipmentRequest, userParams)` | Before an order-based shipment is processed | Order validation, reference injection |
| **PostShipOrder** | `function(shipmentRequest, shipmentResponse)` | After an order-based shipment completes | Order confirmation, status update |

### Load / Scan

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreLoad** | `function(loadValue, shipmentRequest, userParams)` | Before a barcode/scan value is processed | Show loader/spinner, capture pre-load state, validate scan input |
| **PostLoad** | `function(loadValue, shipmentRequest)` | After the load/scan value is resolved | Hide loader, restore post-load state, show load errors, init consolidated orders |

### Rate

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreRate** | `function(shipmentRequest, userParams)` | Before rating request is sent | Sync notification email, validate required fields, modify service selection |
| **PostRate** | `function(shipmentRequest, rateResults)` | After rating results return | Filter/sort rate results, apply service-symbol mapping, D2M checks, display rate comparison |

### Batch Processing

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreProcessBatch** | `function(batchReference, actions, params)` | Before a batch is processed | Validate batch, set batch parameters |
| **PostProcessBatch** | `function(batchResponse)` | After batch processing completes | Display batch results, trigger batch-level printing |

### Void

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreVoid** | `function(pkg, userParams)` | Before a package is voided | Validate void eligibility, `userParams` can cancel the void |
| **PostVoid** | `function(pkg)` | After a package is voided | Update UI, log void action |

### Print

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PrePrint** | `function(document, localPort)` | Before a document is sent to the printer | Inject ZPL printer defaults, modify label content, redirect to different printer |
| **PostPrint** | `function(document)` | After printing completes | Log print action, trigger secondary prints (delegate labels) |

### Manifest

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreCloseManifest** | `function(manifestItem, userParams)` | Before a manifest is closed/end-of-day | Validate manifest contents, `userParams` can cancel |
| **PostCloseManifest** | `function(manifestItem)` | After manifest is closed | Trigger manifest reports, notifications |

### Transmit

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreTransmit** | `function(transmitItem, userParams)` | Before data is transmitted | Validate transmit data, `userParams` can cancel |
| **PostTransmit** | `function(transmitItem)` | After transmission completes | Log result, notify user |

### History Search

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreSearchHistory** | `function(searchCriteria)` | Before a history search is executed | Filter to current user, set default date range, modify search criteria |
| **PostSearchHistory** | `function(packages)` | After history search returns results | Anonymise costs, add tracking hyperlinks, flag old packages as non-printable, mask sensitive data |

### Address Book

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PostSelectAddressBook** | `function(shipmentRequest, nameaddress)` | After the user selects an address from the address book | Sync notification email, populate custom fields from address data, set cost center |

### Package Operations

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **AddPackage** | `function(shipmentRequest, packageIndex)` | A package is added to the shipment | Copy references from previous package, set default dimensions |
| **CopyPackage** | `function(shipmentRequest, packageIndex)` | A package is duplicated | Clear or modify copied references |
| **RemovePackage** | `function(shipmentRequest, packageIndex)` | A package is removed | Clean up references, recalculate totals |
| **PreviousPackage** | `function(shipmentRequest, packageIndex)` | User navigates to previous package | Save current package state, update UI |
| **NextPackage** | `function(shipmentRequest, packageIndex)` | User navigates to next package | Save current package state, update UI |

### Group Operations

| Hook | Signature | When It Fires | Common Uses |
|---|---|---|---|
| **PreCreateGroup** | `function(group, userParams)` | Before a group is created | Validate group, `userParams` can cancel |
| **PostCreateGroup** | `function(group)` | After a group is created | Log, update UI |
| **PreModifyGroup** | `function(group, userParams)` | Before a group is modified | Validate modification |
| **PostModifyGroup** | `function(group)` | After a group is modified | Log, update UI |
| **PreCloseGroup** | `function(group, userParams)` | Before a group is closed | Validate close action |
| **PostCloseGroup** | `function(group)` | After a group is closed | Log, trigger reports |

---

## SBR Hooks (Server Business Rules)

Server Business Rules are compiled .NET assemblies (DLLs) that run in the ShipExec
server pipeline. They are attached to profiles via the `ServerBusinessRuleId` property.

### Key Characteristics

- **Server-side only:** No DOM, no browser APIs, no JavaScript.
- **Compiled:** Deployed as `.dll` files stored in `ServerBusinessRule.FileBytes`.
- **Per-profile binding:** A Profile's `ServerBusinessRuleId` links to the SBR.
  Multiple profiles can share the same SBR.
- **Pipeline hooks:** SBR hooks mirror many of the same lifecycle events as CBR but
  execute on the server before/after the CBR equivalents.

### SBR Lifecycle Hooks

SBRs implement the same Pre/Post pattern as CBRs but on the server side. The server
invokes these hooks during request processing:

| Hook | Execution Point | Relationship to CBR |
|---|---|---|
| **PreShip** | Before the shipment is processed by the carrier adapter | Runs BEFORE the CBR `PreShip`; can modify/reject the request server-side |
| **PostShip** | After the carrier returns a response | Runs AFTER carrier response, BEFORE CBR `PostShip` |
| **PreRate** | Before rating is executed | Server-side rate request modification |
| **PostRate** | After rating results are returned | Server-side rate result filtering/modification |
| **PreVoid** | Before a void is executed | Server-side void validation |
| **PostVoid** | After a void completes | Server-side void logging/cleanup |
| **PrePrint** | Before label/document generation | Server-side document modification |
| **PostPrint** | After label/document generation | Server-side post-print processing |
| **PreCloseManifest** | Before manifest close-out | Server-side manifest validation |
| **PostCloseManifest** | After manifest close-out | Server-side manifest finalization |
| **PreTransmit** | Before data transmission | Server-side transmit validation |
| **PostTransmit** | After data transmission | Server-side transmit logging |

### SBR Properties

| Property | Type | Description |
|---|---|---|
| `Id` | `int` | Unique identifier |
| `Name` | `string` | Display name |
| `Description` | `string?` | Human-readable description |
| `Version` | `string?` | Version label |
| `Author` | `string?` | Author name |
| `AuthorEmail` | `string?` | Author email |
| `CompanyId` | `Guid` | Owning company |
| `FileBytes` | `byte[]` | Compiled DLL binary |

---

## Execution Order (CBR + SBR Combined)

For operations that have both server and client hooks, the typical execution order is:

```
1. [Client]  CBR Pre* hook fires (e.g. PreShip)
     ↓  userParams can cancel here → abort
2. [Client]  Request sent to server
3. [Server]  SBR Pre* hook fires
     ↓  server can modify/reject → error response
4. [Server]  Core processing (carrier API, rating engine, etc.)
5. [Server]  SBR Post* hook fires
     ↓  server can modify response
6. [Client]  Response received
7. [Client]  CBR Post* hook fires (e.g. PostShip)
```

---

## CBR Utility Modules Available Inside Hooks

These modules are available when using the Enhanced CBR Template:

| Module | Purpose |
|---|---|
| `CbrLogger` | Structured multi-level logging (Info/Debug/Trace/Error/Fatal), optional server-side logging |
| `ThinClientApi` | Unified `$.ajax` wrapper for ShipExec server requests (`UserMethod`, `GetOrderInformation`, etc.) |
| `UserContextFilter` | Retrieve current user context, filter history by user |
| `EventHandling` | Default no-op hooks, page routing helpers, shipper sorting, DOM helpers |
| `FieldValidation` | DOM polling for Angular-bound fields, custom-data attribute access, input validation |
| `TimestampUtils` | Date/time stamp generation, stamp all packages on a given MiscReference field |
| `AddressBookLogic` | Consignee matching, reference field updates from address data |
| `AddressBookLookup` | Code-search Tab binding, return-address population |
| `ShippingDefaults` | Address population, notification email sync, cost centre loading |
| `ServiceFiltering` | Service-symbol mapping, D2M (Drop-to-Manifest) checks |
| `CommodityHandling` | UOM translation, weight-based service selection |
| `ShipmentValidation` | Reference composition, dimension/weight validation, package reference copying |
| `LoadStateManager` | Loader/spinner show/hide, PreLoad/PostLoad state capture/restore |
| `BatchHistoryOps` | Batch/void/history hook helpers, date range defaults |
| `PackageHistoryAnon` | Cost anonymisation, tracking links, old-package flagging |
| `PrintingOutput` | ZPL injection, delegate labels, file I/O |
| `CustomBatchProcessing` | Consolidated-order dialog, batch helpers |
| `ThirdPartyOptions` | Profile CRUD, manifest close-out helpers |

---

## `userParams` Cancellation Pattern

Several `Pre*` hooks receive a `userParams` object. Setting properties on this object
can cancel the operation:

```javascript
this.PreShip = function (shipmentRequest, userParams) {
    if (!isValid(shipmentRequest)) {
        userParams.Cancel = true;
        userParams.CancelMessage = "Validation failed: missing required field.";
    }
};
```

Hooks that support `userParams` cancellation:
- `PreShip`
- `PreLoad`
- `PreRate`
- `PreVoid`
- `PreShipOrder`
- `PreCloseManifest`
- `PreTransmit`
- `PreProcessBatch`
- `PreCreateGroup`
- `PreModifyGroup`
- `PreCloseGroup`

---

## `Tools.*` API (Available in CBR Hooks)

Key methods available on the global `Tools` object inside CBR scripts:

| Method | Description |
|---|---|
| `Tools.GetCurrentUserContext()` | Returns the current user context object |
| `Tools.ShowLoader()` / `Tools.HideLoader()` | Show/hide the loading spinner |
| `Tools.SetCurrentViewModel(vm, ...)` | Set the current Angular view model |
| `Tools.MakeUserMethodRequest(method, data, success, error)` | Call a server-side UserMethod |
| `Tools.FocusOnLoadInput` | Focus the load/scan input field |

---

## Prompt Usage

When using this document as context for an AI prompt, include it as a system message:

```
You are an expert in ShipExec Business Rules. Use the following hook reference
to understand the complete CBR and SBR lifecycle. When generating or analysing
business rule scripts, ensure hooks are used correctly per their signatures,
execution timing, and available parameters.

[paste this document]
```

---

## Detailed Program Flow & Branching Operations

This section documents the complete execution flow for each major shipping operation,
including all decision points, branching logic, and the interplay between CBR and SBR.

### Ship Operation — Complete Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ USER CLICKS "SHIP" BUTTON                                       │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PreBuildShipment(shipmentRequest)                           │
│  • Inject field defaults before form data is assembled          │
│  • Modify shipmentRequest properties                            │
│  • NO cancellation available at this stage                      │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ THIN CLIENT: Assembles shipmentRequest from DOM/Angular model    │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostBuildShipment(shipmentRequest)                          │
│  • Validate assembled data                                      │
│  • Modify computed fields after assembly                        │
│  • NO cancellation available at this stage                      │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PreShip(shipmentRequest, userParams)                        │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF validation fails                                         │
│  │   → userParams.Cancel = true                                 │
│  │   → userParams.CancelMessage = "reason"                      │
│  │   → FLOW STOPS — shipment not sent to server                 │
│  │                                                              │
│  ├─ IF field enrichment needed                                  │
│  │   → Modify shipmentRequest.Packages[n].MiscReferenceX        │
│  │   → Stamp timestamps, compose references                    │
│  │   → FLOW CONTINUES                                           │
│  │                                                              │
│  └─ IF no intervention needed                                   │
│      → FLOW CONTINUES                                           │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
                     ┌──── userParams.Cancel? ────┐
                     │ YES                   NO   │
                     ▼                        ▼
        ┌─────────────────┐    ┌──────────────────────────────┐
        │ Show error msg  │    │ HTTP POST to server           │
        │ Ship aborted    │    │ (ShipExec Service endpoint)   │
        └─────────────────┘    └──────────────┬───────────────┘
                                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PreShip (Server-side .NET)                                  │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF server-side validation fails                             │
│  │   → Throw exception / return error response                  │
│  │   → FLOW STOPS — error returned to client                   │
│  │                                                              │
│  ├─ IF data enrichment needed                                   │
│  │   → Modify request (add references, lookup DB values)        │
│  │   → FLOW CONTINUES to carrier                               │
│  │                                                              │
│  └─ IF routing/carrier override needed                          │
│      → Modify carrier selection, service type                   │
│      → FLOW CONTINUES to carrier                               │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CORE: Carrier API call (UPS, FedEx, DHL, etc.)                  │
│  • Sends shipment to carrier                                    │
│  • Receives tracking number, label, rates                       │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PostShip (Server-side .NET)                                 │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF response needs enrichment                                │
│  │   → Add custom data to response                             │
│  │   → Write to external DB / file system                      │
│  │                                                              │
│  ├─ IF notification needed                                      │
│  │   → Trigger email/webhook                                   │
│  │                                                              │
│  └─ IF logging/audit required                                   │
│      → Write audit trail                                       │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ Response returned to client (JSON)                              │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostShip(shipmentRequest, shipmentResponse)                 │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF shipmentResponse.Success == true                         │
│  │   → Display tracking number                                 │
│  │   → Trigger delegate label printing                         │
│  │   → Write to localStorage / file                            │
│  │                                                              │
│  └─ IF shipmentResponse.Success == false                        │
│      → Display error to user                                   │
│      → Log failure details                                     │
└─────────────────────────────────────────────────────────────────┘
```

### Load/Scan Operation — Complete Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ USER SCANS BARCODE / ENTERS LOAD VALUE                          │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PreLoad(loadValue, shipmentRequest, userParams)             │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF loadValue format is invalid                              │
│  │   → userParams.Cancel = true                                 │
│  │   → userParams.CancelMessage = "Invalid barcode format"      │
│  │   → FLOW STOPS                                              │
│  │                                                              │
│  ├─ IF loader/spinner should show                               │
│  │   → Tools.ShowLoader()                                      │
│  │   → Capture pre-load state (current field values)           │
│  │   → FLOW CONTINUES                                          │
│  │                                                              │
│  └─ IF loadValue needs transformation                           │
│      → Parse barcode, extract order ID                         │
│      → Modify loadValue or shipmentRequest fields              │
│      → FLOW CONTINUES                                          │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
                     ┌──── userParams.Cancel? ────┐
                     │ YES                   NO   │
                     ▼                        ▼
        ┌─────────────────┐    ┌──────────────────────────────┐
        │ Show error msg  │    │ Thin Client resolves load     │
        │ Load aborted    │    │ (address lookup, order fetch) │
        └─────────────────┘    └──────────────┬───────────────┘
                                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostLoad(loadValue, shipmentRequest)                        │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF load returned an error                                   │
│  │   → Display error notification                              │
│  │   → Restore pre-load field state                            │
│  │                                                              │
│  ├─ IF consolidated order mode                                  │
│  │   → Init consolidated order elements                        │
│  │   → Show order dialog                                       │
│  │                                                              │
│  └─ IF normal load success                                      │
│      → Tools.HideLoader()                                      │
│      → Focus next input field                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Rate Operation — Complete Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ USER CLICKS "RATE" or SYSTEM AUTO-RATES                         │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PreRate(shipmentRequest, userParams)                        │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF required fields missing                                  │
│  │   → userParams.Cancel = true                                 │
│  │   → FLOW STOPS                                              │
│  │                                                              │
│  ├─ IF notification email needs sync                            │
│  │   → Read email from DOM, write to shipmentRequest            │
│  │   → FLOW CONTINUES                                          │
│  │                                                              │
│  └─ IF service filter needed                                    │
│      → Restrict available services based on weight/dest         │
│      → FLOW CONTINUES                                          │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PreRate (Server-side)                                       │
│  • Server-side rate request modification                        │
│  • May add/remove carriers from rating pool                     │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CORE: Rating engine queries all applicable carriers             │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PostRate (Server-side)                                      │
│  • Filter/sort rate results server-side                         │
│  • Apply business discounts or surcharges                       │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostRate(shipmentRequest, rateResults)                      │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF rate results empty                                       │
│  │   → Show "no rates available" message                       │
│  │                                                              │
│  ├─ IF D2M (Drop-to-Manifest) check needed                     │
│  │   → Filter results by D2M eligibility                       │
│  │   → Map service symbols to display names                    │
│  │                                                              │
│  └─ IF rate comparison display needed                           │
│      → Sort by price, show comparison UI                       │
└─────────────────────────────────────────────────────────────────┘
```

### History Search — Complete Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ USER NAVIGATES TO HISTORY PAGE / CLICKS SEARCH                  │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PreSearchHistory(searchCriteria)                            │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF user-level filtering required                            │
│  │   → Set searchCriteria.UserId = current user                │
│  │   → Users can only see their own shipments                  │
│  │                                                              │
│  ├─ IF date range restriction needed                            │
│  │   → Set searchCriteria.StartDate = today - N days           │
│  │   → Prevent searching too far back                          │
│  │                                                              │
│  └─ IF custom field filter needed                               │
│      → Add WHERE clause to searchCriteria                      │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SERVER: Executes history query                                  │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostSearchHistory(packages)                                 │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF cost anonymisation required                              │
│  │   → Replace all cost fields with "***"                      │
│  │   → Non-admin users cannot see shipping costs               │
│  │                                                              │
│  ├─ IF tracking links needed                                    │
│  │   → Inject clickable tracking URLs into results             │
│  │                                                              │
│  ├─ IF old packages should be non-printable                     │
│  │   → Flag packages older than N days                         │
│  │   → Disable reprint button for those                        │
│  │                                                              │
│  └─ IF no modifications needed                                  │
│      → Display results as-is                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Print Operation — Complete Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ SYSTEM TRIGGERS PRINT (auto or manual)                          │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PrePrint (Server-side)                                      │
│  • Modify document content before generation                    │
│  • Inject custom label fields                                   │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CORE: Label/document generation                                 │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PostPrint (Server-side)                                     │
│  • Post-generation processing                                   │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PrePrint(document, localPort)                               │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF document is ZPL label                                    │
│  │   → Inject ZPL printer defaults (darkness, speed)           │
│  │   → Set correct label format identifier                     │
│  │                                                              │
│  ├─ IF redirect to different printer                            │
│  │   → Override localPort parameter                            │
│  │                                                              │
│  └─ IF no print modification needed                             │
│      → Pass through unchanged                                  │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ PRINT: Document sent to printer/port                            │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostPrint(document)                                         │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF delegate labels needed                                   │
│  │   → Generate and print additional copies                    │
│  │                                                              │
│  ├─ IF file output needed                                       │
│  │   → Write label data to file system                         │
│  │                                                              │
│  └─ IF logging needed                                           │
│      → Log print completion                                    │
└─────────────────────────────────────────────────────────────────┘
```

### Void Operation — Complete Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ USER CLICKS "VOID" ON A PACKAGE                                 │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PreVoid(pkg, userParams)                                    │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF package too old to void                                  │
│  │   → userParams.Cancel = true                                 │
│  │   → userParams.CancelMessage = "Cannot void after 24hrs"    │
│  │   → FLOW STOPS                                              │
│  │                                                              │
│  ├─ IF user lacks void permission                               │
│  │   → userParams.Cancel = true                                 │
│  │   → FLOW STOPS                                              │
│  │                                                              │
│  └─ IF void allowed                                             │
│      → FLOW CONTINUES                                          │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PreVoid (Server-side)                                       │
│  • Additional server-side void validation                       │
│  • Check carrier void window                                    │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CORE: Void request sent to carrier                              │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PostVoid (Server-side)                                      │
│  • Update database records                                      │
│  • Trigger refund process if applicable                         │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostVoid(pkg)                                               │
│  • Update UI to reflect voided status                           │
│  • Log void action                                              │
└─────────────────────────────────────────────────────────────────┘
```

### Batch Processing — Complete Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ USER INITIATES BATCH PROCESSING                                 │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PreProcessBatch(batchReference, actions, params)            │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF batch validation fails                                   │
│  │   → params.Cancel = true                                    │
│  │   → FLOW STOPS                                              │
│  │                                                              │
│  └─ IF batch needs parameter injection                          │
│      → Modify actions or params                                │
│      → FLOW CONTINUES                                          │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SERVER: Process each item in batch                              │
│  ├─ FOR EACH item in batch:                                     │
│  │   → SBR PreShip (per-item)                                  │
│  │   → Carrier API call                                        │
│  │   → SBR PostShip (per-item)                                 │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostProcessBatch(batchResponse)                             │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF all items succeeded                                      │
│  │   → Show success summary                                   │
│  │   → Trigger batch-level printing                            │
│  │                                                              │
│  ├─ IF some items failed                                        │
│  │   → Show partial success with error details                 │
│  │   → Highlight failed items                                  │
│  │                                                              │
│  └─ IF all items failed                                         │
│      → Show batch failure message                              │
└─────────────────────────────────────────────────────────────────┘
```

### Manifest Close-Out — Complete Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ USER TRIGGERS END-OF-DAY / MANIFEST CLOSE                       │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PreCloseManifest(manifestItem, userParams)                  │
│                                                                 │
│  BRANCHING:                                                     │
│  ├─ IF manifest has unresolved issues                           │
│  │   → userParams.Cancel = true                                 │
│  │   → Show warning about open items                           │
│  │   → FLOW STOPS                                              │
│  │                                                              │
│  └─ IF manifest OK to close                                     │
│      → FLOW CONTINUES                                          │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PreCloseManifest (Server-side)                              │
│  • Validate all packages in manifest                            │
│  • Check carrier-specific close-out rules                       │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CORE: Manifest close-out with carrier                           │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ SBR: PostCloseManifest (Server-side)                             │
│  • Generate end-of-day reports                                  │
│  • Archive manifest data                                        │
└───────────────────────────────────┬─────────────────────────────┘
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│ CBR: PostCloseManifest(manifestItem)                             │
│  • Show confirmation to user                                    │
│  • Trigger manifest report print                                │
│  • Reset UI for next day                                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## Common Branching Patterns

### Pattern 1: Validation Gate (Cancel if invalid)

```javascript
this.PreShip = function (shipmentRequest, userParams) {
    // Gate 1: Required field check
    if (!shipmentRequest.Packages[0].ShipperReference) {
        userParams.Cancel = true;
        userParams.CancelMessage = "Shipper Reference is required.";
        return;  // early exit
    }

    // Gate 2: Weight validation
    for (var i = 0; i < shipmentRequest.Packages.length; i++) {
        if (shipmentRequest.Packages[i].Weight <= 0) {
            userParams.Cancel = true;
            userParams.CancelMessage = "Package " + (i+1) + " has invalid weight.";
            return;
        }
    }

    // Gate 3: Dimension check (conditional)
    if (shipmentRequest.ServiceSymbol === 'FREIGHT') {
        for (var i = 0; i < shipmentRequest.Packages.length; i++) {
            var pkg = shipmentRequest.Packages[i];
            if (!pkg.Length || !pkg.Width || !pkg.Height) {
                userParams.Cancel = true;
                userParams.CancelMessage = "Dimensions required for freight.";
                return;
            }
        }
    }

    // All gates passed — enrichment
    TimestampUtils.stampAllPackages(shipmentRequest, 'MiscReference15');
};
```

### Pattern 2: Conditional Enrichment (Modify based on conditions)

```javascript
this.PostLoad = function (loadValue, shipmentRequest) {
    Tools.HideLoader();

    // Branch on load result
    if (shipmentRequest.LoadError) {
        // ERROR PATH
        alert(shipmentRequest.LoadError);
        LoadStateManager.restorePreLoadState(shipmentRequest);
    } else if (shipmentRequest.IsConsolidatedOrder) {
        // CONSOLIDATED ORDER PATH
        ConsolidatedOrders.InitElements(true);
        CustomBatchProcessing.showOrderDialog();
    } else {
        // NORMAL PATH
        ShippingDefaults.syncNotificationEmailFromDom(shipmentRequest);
        EventHandling.focusNextField();
    }
};
```

### Pattern 3: Page-Based Routing (Different logic per page)

```javascript
this.PageLoaded = function (location) {
    switch (location) {
        case '#!/shipping':
            EventHandling.sortShippers(vm);
            AddressBookLookup.bindTabSearch();
            ShippingDefaults.loadCostCenter(vm);
            break;

        case '#!/history':
            PackageHistoryAnon.setupTrackingLinks();
            break;

        case '#!/batchdetail':
            CustomBatchProcessing.initBatchView();
            break;

        default:
            // No page-specific init
            break;
    }
};
```

### Pattern 4: Loop with Per-Package Logic

```javascript
this.PreShip = function (shipmentRequest, userParams) {
    for (var i = 0; i < shipmentRequest.Packages.length; i++) {
        var pkg = shipmentRequest.Packages[i];

        // Branch: weight-based service override
        if (pkg.Weight > 150) {
            shipmentRequest.ServiceSymbol = 'FREIGHT';
        }

        // Branch: international requires commodity info
        if (shipmentRequest.IsInternational && !pkg.CommodityDescription) {
            userParams.Cancel = true;
            userParams.CancelMessage = "Package " + (i+1) + ": Commodity description required for international.";
            return;
        }

        // Always: stamp timestamp
        pkg.MiscReference15 = TimestampUtils.generate();
    }
};
```

### Pattern 5: Async Server Call with Callback (CBR only)

```javascript
this.NewShipment = function (shipmentRequest) {
    // Async pattern — call server, handle response in callback
    Tools.MakeUserMethodRequest(
        'GetCostCenter',
        JSON.stringify({ userId: Tools.GetCurrentUserContext().UserId }),
        function (response) {
            // SUCCESS PATH
            if (response && response.CostCenter) {
                vm.currentShipment.CostCenter = response.CostCenter;
            }
        },
        function (error) {
            // ERROR PATH
            CbrLogger.log(CbrLogger.LogLevel.Error, 'GetCostCenter', error);
        }
    );
};
```

---

## Decision Matrix: CBR vs SBR

Use this matrix to determine where a business rule should be implemented:

| Criterion | Use CBR | Use SBR | Use Both |
|---|---|---|---|
| **Needs DOM/UI access** | ✅ | ❌ | CBR for UI, SBR for logic |
| **Must be tamper-proof** | ❌ | ✅ | SBR enforces, CBR shows UX |
| **Real-time user feedback** | ✅ | ❌ | CBR for UX, SBR for enforcement |
| **Database lookup required** | ❌ | ✅ | SBR does lookup, CBR uses result |
| **Carrier-specific logic** | ❌ | ✅ | — |
| **Field default population** | ✅ | ❌ | — |
| **Cost/rate manipulation** | ❌ | ✅ | SBR modifies, CBR displays |
| **Print/label modification** | Both | Both | CBR for ZPL, SBR for content |
| **User context filtering** | ✅ | ❌ | — |
| **Audit/compliance logging** | ❌ | ✅ | SBR logs, CBR shows status |
| **External API integration** | ❌ | ✅ | — |
| **Address book manipulation** | ✅ | ❌ | — |
| **Barcode/scan handling** | ✅ | ❌ | CBR parses, SBR validates |
