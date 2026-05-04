# ShipExec Templates & CBR Customization Guide

## Purpose

You are an AI assistant helping a developer implement **UI customizations** for the ShipExec Thin Client shipping application. You will be given a blueprint document describing custom layout, field, or behavior changes for a specific company. Your job is to:

1. Identify which template file(s) need modification
2. Generate the modified HTML template code
3. Generate any accompanying CBR (Client Business Rules) JavaScript code
4. Explain what hooks or ViewModel properties are involved

---

## Architecture Overview

The ShipExec Thin Client is an **AngularJS 1.x** single-page application. The UI is composed of HTML templates that use AngularJS directives and bind to a ViewModel (`vm`). Customization happens at two levels:

- **HTML Templates** – Define layout, fields, buttons, and visibility. Deployed per-company on the ShipExec server. Each template file maps to a specific screen/section of the application.
- **CBR (Client Business Rules)** – JavaScript that responds to UI events (hooks). Can manipulate the ViewModel, add/remove fields, show/hide elements, validate data, and call server APIs.

Templates use AngularJS binding syntax:
- `{{vm.property}}` – One-way display binding
- `ng-model="vm.property"` – Two-way data binding
- `ng-click="vm.method()"` – Click handler
- `ng-show/ng-hide="expression"` – Conditional visibility
- `ng-repeat="item in vm.collection"` – Iteration
- `ng-disabled="expression"` – Disable control
- `ng-class="{'class': condition}"` – Conditional CSS class

---

## Template File Inventory

There are **16 template files**. Each is described below with its full structure, purpose, ViewModel bindings, and customization points.

---

### 1. `shippingTemplate.html` (322 lines)

**Purpose:** The PRIMARY shipping screen. This is where 90% of day-to-day shipping happens.

**Screen Layout:**
```
┌─────────────────────────────────────────────────────────────────────────┐
│ [New(F2)] [Repeat(F8)] [Build] [DistList] [Pickup] [Pending] [MailRoom] │
│                                          Mode: [Standard▼] Pkg: 1 of 1  │
├────────────────────┬────────────────────────────────────────────────────┤
│  LEFT COLUMN (4)   │  RIGHT COLUMN (8)                                  │
│                    │                                                    │
│  Load From: [___]  │  ┌─────────────────────────────────────────────┐  │
│  Load: [____](F7)  │  │ Tabs: General | Accessorials | Intl | Hazmat│  │
│  ─────────────     │  │       Reference | LTL | Visibility | Goods  │  │
│  Shipper: [___]    │  │                                             │  │
│  Ship Date: [___]  │  │  (Tab content - package details)            │  │
│  Terms: [___]      │  │                                             │  │
│  ☐ 3rd Party       │  └─────────────────────────────────────────────┘  │
│  ☐ Bill Consignee  │                                                    │
│  ☐ Brokerage       │                                                    │
│                    │                                                    │
│  Address Tabs:     │                                                    │
│  [Consignee]       │                                                    │
│  [Return Address]  │                                                    │
│  [Origin]          │                                                    │
│  [3rd Party Addr]  │                                                    │
│  [Consignee 3rd]   │                                                    │
│  [Brokerage Addr]  │                                                    │
│                    │                                    [Save][Rate][Ship]│
├────────────────────┴────────────────────────────────────────────────────┤
│  (Optional: Last Shipment Panel - right side, tracking #, costs)        │
└─────────────────────────────────────────────────────────────────────────┘
```

**Keyboard Shortcuts:**
| Key | Action | VM Method |
|-----|--------|-----------|
| F2 | New Shipment | `vm.newShipment()` |
| F7 | Load | `vm.load()` |
| F8 | Repeat Shipment | `vm.repeatShipment()` |
| F9 | Rate | `vm.rate()` |
| F12 | Ship | `vm.checkShip()` |

**Key ViewModel Properties:**
| Property | Type | Purpose |
|----------|------|---------|
| `vm.loadValue` | string | The scan/order number entered in Load field |
| `vm.loadFrom` | string | Load source selection (Business Rule, Batch, Mail Room) |
| `vm.currentShipment` | ShipmentRequest | The entire shipment being built |
| `vm.currentShipment.PackageDefaults.Shipper` | string | Selected shipper symbol |
| `vm.currentShipment.PackageDefaults.Consignee` | NameAddress | Consignee address object |
| `vm.currentShipment.PackageDefaults.ReturnAddress` | NameAddress | Return/ship-from address |
| `vm.currentShipment.PackageDefaults.Service` | string | Selected service symbol |
| `vm.currentShipment.PackageDefaults.Terms` | string | Payment terms symbol |
| `vm.currentShipment.PackageDefaults.ThirdPartyBilling` | bool | 3rd party billing enabled |
| `vm.currentShipment.PackageDefaults.ThirdPartyBillingAddress` | NameAddress | 3rd party address |
| `vm.currentShipment.Packages[]` | array | Package array |
| `vm.packageIndex` | int | Currently displayed package index |
| `vm.totalPackages` | int | Total package count |
| `vm.shipMode` | int | Shipping mode (Standard, Multi-piece, etc.) |
| `vm.profile` | object | User profile (shippers, services, printers, field options) |
| `vm.profile.FieldOptions.*` | object | Controls field visibility/captions |
| `vm.profile.ProfileSetting.*` | object | System settings |
| `vm.profile.Shippers` | array | Available shippers |
| `vm.profile.PaymentTerms` | array | Available payment terms |
| `vm.lastShipmentResponse` | ShipmentResponse | Last shipped package info |
| `vm.showLastShipmentPanel` | bool | Show/hide last shipment sidebar |
| `vm.startshipdate` | object | Ship date picker state |
| `vm.showDistributionList` | bool | Show distribution list mode |

**Custom Directives Used:**
| Directive | Purpose | Key Attributes |
|-----------|---------|----------------|
| `<keyboard-event>` | Bind keyboard shortcut | `key`, `on-keypress` |
| `<field-select>` | Dropdown field | `property`, `model`, `options`, `caption`, `change-function`, `form-type` |
| `<field-date>` | Date picker field | `property`, `model`, `caption`, `is-open`, `datepicker-options` |
| `<field-checkbox>` | Checkbox field | `property`, `model`, `caption`, `change` |
| `<name-address>` | Full address entry block | `nameaddress`, `countries`, `hide-*`, `field-options`, `address-property-name` |
| `<general-tab>` | Package details (weight, dims, box type, declared value) | — |
| `<accessorial-tab>` | Accessorial services (signature, COD, etc.) | — |
| `<international-tab>` | International/customs data | — |
| `<hazmat-tab>` | Hazardous materials | — |
| `<reference-tab>` | Reference numbers (shipper ref, consignee ref, misc refs) | — |
| `<ltl-tab>` | LTL/freight details | — |
| `<visibility-tab>` | Visibility/notification settings | — |
| `<goods-tab>` | Commodity/goods declarations | — |

**Right-Column Tabs (index 0-7):**
| Index | Tab | FieldOptions Key | Content Directive |
|-------|-----|-----------------|-------------------|
| 0 | General | `General` | `<general-tab>` |
| 1 | Accessorials | `Accessorials` | `<accessorial-tab>` |
| 2 | International | `Intl` | `<international-tab>` |
| 3 | Hazmat | `Hazmat` | `<hazmat-tab>` |
| 4 | Reference | `Reference` | `<reference-tab>` |
| 5 | LTL | `LTL` | `<ltl-tab>` |
| 6 | Visibility | `Visibility` | `<visibility-tab>` |
| 7 | Goods | `Goods` | `<goods-tab>` |

**Left-Column Address Tabs (index 0-6):**
| Index | Tab | Shows When |
|-------|-----|------------|
| 0 | Consignee | Not in distribution list mode, not hidden |
| 1 | Distribution List | `vm.showDistributionList == true` |
| 2 | Return Address | `FieldOptions.ReturnAddress.IsHidden == false` |
| 3 | Origin | `FieldOptions.OriginAddress.IsHidden == false` |
| 4 | Third Party Billing | `ThirdPartyBilling == true` |
| 5 | Consignee Third Party | `ConsigneeThirdPartyBilling == true` |
| 6 | Brokerage Third Party | `BrokerageThirdPartyBilling == true` |

**Bottom Action Buttons:**
| Button | Condition | Action |
|--------|-----------|--------|
| Save | `!DisablePendingShipment` | `vm.saveShipment()` |
| Rate (F9) | ServiceSelectionMode allows | `vm.rate()` |
| Ship (F12) | `!DisableShip` | `vm.checkShip()` |
| Create License Plate | `EnableLicensePlate` | `vm.saveLicensePlate()` |
| Ship Distribution List | `showDistributionList` | `vm.shipDistributionList()` |

**Last Shipment Panel (right sidebar):**
Shows when `vm.showLastShipmentPanel == true`. Displays:
- Total packages, Service name
- Base/Discount/Special/Total charges
- Tracking number(s), GlobalMSN(s)
- Actions: View details, Reprint, Void

**Common Customization Points:**
- Add/remove/reorder buttons in the top toolbar
- Add custom fields between existing fields
- Hide sections via `ng-hide`/`ng-show`
- Add new address tabs
- Modify the Load field behavior
- Add custom keyboard shortcuts
- Change column widths (col-md-4 / col-md-8 split)
- Add custom content to the Last Shipment panel

**Related Hooks (CBR):**
- `PageLoaded('/shipping')` — Initialize shipping page
- `NewShipment(shipmentRequest)` — Set defaults for new shipment
- `PreLoad/PostLoad` — Before/after load
- `PreBuildShipment/PostBuildShipment` — Before/after UI → object
- `PreShip/PostShip` — Before/after ship
- `Keystroke` — Any key pressed

---

### 2. `scanshipTemplate.html` (83 lines)

**Purpose:** Simplified "Scan & Ship" mode — one-shot load+ship. User enters an order number and the system loads AND ships in a single action.

**Screen Layout:**
```
┌─────────────────────────────────────────┐
│        Scan and Ship                     │
│                                         │
│  Ship Date: [__________] 📅             │
│                                         │
│  Order Number: [__________] [Process]   │
│                              [Clear]    │
│                                         │
├─────────────────────────────────────────┤
│  Last Track: ORDER123                   │
│                         [🔍] [🖨️]       │
└─────────────────────────────────────────┘
```

**Keyboard Shortcuts:**
| Key | Action |
|-----|--------|
| Enter | `vm.process()` (Load + Ship) |
| F2 | `vm.newShipment()` (Clear) |
| ESC | `vm.newShipment()` (Clear) |

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.orderNumber` | The scanned order number |
| `vm.shipDate.value` | Ship date |
| `vm.lastOrderTracked` | Last successfully processed order |

**Key Actions:**
| Method | Purpose |
|--------|---------|
| `vm.process()` | Calls Load then Ship automatically |
| `vm.newShipment()` | Clears the form |
| `vm.viewShipmentDetail()` | View last shipment details |
| `vm.reprint()` | Reprint last label |

**Common Customization Points:**
- Add additional fields (e.g., weight override, service selection)
- Change the panel title/caption
- Add validation messages
- Modify the footer to show more last-shipment details
- Add a service/carrier selector if the customer needs to choose at scan time

**Related Hooks (CBR):**
- `PreLoad/PostLoad` — Load behavior
- `PreShip/PostShip` — Ship behavior
- `Keystroke` — Can intercept Enter key

---

### 3. `loadTemplate.html` (71 lines)

**Purpose:** Batch loading panel — appears inside `batchmanagerTemplate.html`. Provides two modes for loading batches: Custom Load (from database via SBR) or Load from File (upload CSV/XML/JSON).

**Screen Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│ ○ Custom Load    ○ Load from File                               │
├─────────────────────────────────────────────────────────────────┤
│ [Custom Load mode:]                                             │
│  Available Batches: [dropdown▼]     Stage and:                  │
│  -OR-                               ☐ Ship                     │
│  Enter Batch Reference: [____]      ☐ Print                    │
│                                     [Process]                   │
├─────────────────────────────────────────────────────────────────┤
│ [Load from File mode:]                                          │
│  File Type: [ShipExecXml▼]  [Choose File] [Upload]             │
│  Download template for ShipExecXml format                       │
└─────────────────────────────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.loadType` | Selected mode (CustomLoad or LoadFromFile) |
| `vm.batchLoadTypes.CustomLoad` | Enum value for custom load |
| `vm.batchLoadTypes.LoadFromFile` | Enum value for file load |
| `vm.selectedBatchReference` | Selected/entered batch reference |
| `vm.batchReferences` | Available batch references from `GetBatchReferences()` |
| `vm.entryMode` | 'select' (dropdown) or 'manual' (text input) |
| `vm.actions.ship` | Stage and ship checkbox |
| `vm.actions.print` | Stage and print checkbox |
| `vm.parseBatchType` | File parser type (ShipExecXml, ShipExecJson, CompleteViewShipping) |
| `vm.batchFile` | Uploaded file |

**Key Actions:**
| Method | Purpose |
|--------|---------|
| `vm.process(vm.actions)` | Process the selected batch |
| `vm.uploadBatchFile()` | Upload and parse a batch file |

**Profile Settings that Control Behavior:**
| Setting | Effect |
|---------|--------|
| `DisableCustomLoad` | Disables the Custom Load radio/controls |
| `DisableLoadFromFile` | Disables the File Load radio/controls |
| `DisableBatchFileParserType` | Disables changing file type dropdown |

**Related Hooks:**
- **SBR:** `GetBatchReferences()` — populates the dropdown
- **SBR:** `LoadBatch()` — loads from custom source
- **SBR:** `ParseBatchFile()` — parses uploaded file
- **CBR:** `PreProcessBatch/PostProcessBatch`

---

### 4. `batchmanagerTemplate.html` (140 lines)

**Purpose:** Batch Manager — the main batch list screen. Shows all loaded batches with their status and provides actions.

**Screen Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│  Batch Manager                                                  │
├─────────────────────────────────────────────────────────────────┤
│  [loadTemplate embedded here]                                   │
├─────────────────────────────────────────────────────────────────┤
│  Batches                                    [10][20][50][All]   │
│ ┌───────────────────────────────────────────────────────────┐   │
│ │   | Batch Reference | Status | Errors | Shipped | Total | │   │
│ │   | [search______🔍]|        |        |         |       | │   │
│ │   | BATCH001        | Ready  | 0      | 5       | 10    | │   │
│ │   |                 |        |        |         | [Ship][Print][Void][Delete] │
│ └───────────────────────────────────────────────────────────┘   │
│  Showing 1 to 10 of 25 entries    [< 1 2 3 >]                  │
└─────────────────────────────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.batches` | Array of batch objects |
| `vm.searchForBatchReference` | Search filter text |
| `vm.itemsPerPage` | Pagination size |
| `vm.currentPage` | Current page |
| `vm.totalItems` | Total batch count |

**Per-Batch Object:**
| Property | Purpose |
|----------|---------|
| `batch.Info.BatchReference` | Batch identifier |
| `batch.Info.BatchStatus` | Status (uses `BatchStatus` filter) |
| `batch.Info.TotalErrors` | Error count |
| `batch.Info.TotalShipped` | Shipped count |
| `batch.Info.TotalItems` | Total items |
| `batch.CanShip()` | Can this batch be shipped |
| `batch.CanPrint()` | Can this batch be printed |
| `batch.CanVoid()` | Can this batch be voided |
| `batch.CanDelete()` | Can this batch be deleted |
| `batch.IsBusy()` | Is this batch processing |

**Key Actions:**
| Method | Purpose |
|--------|---------|
| `vm.getBatches(false)` | Search/refresh batch list |
| `vm.linkToDetails(batchReference)` | Navigate to batch detail |
| `vm.shipBatch(batch)` | Ship entire batch |
| `vm.printBatch(batch)` | Print entire batch |
| `vm.voidBatch(batch)` | Void entire batch |
| `vm.deleteBatch(batch)` | Delete entire batch |

---

### 5. `batchdetailTemplate.html` (125 lines)

**Purpose:** Batch Detail — shows individual items within a single batch. Allows per-item and bulk actions.

**Screen Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│  Batch Details                                                  │
│  [← Batch Manager]                                              │
│                                                                 │
│  Batch Reference: BATCH001 (link to history)                    │
│  Status: Ready                    [Processing...]               │
│                                                                 │
│  ☐ Show Errors Only        [Ship All][Print All][Void All][Delete All] │
├─────────────────────────────────────────────────────────────────┤
│  Batch Details                                  [10][20][50][All]│
│ ┌───────────────────────────────────────────────────────────┐   │
│ │  Table of individual batch items with per-row actions      │   │
│ └───────────────────────────────────────────────────────────┘   │
│  Showing 1 to 10 of 50 entries    [< 1 2 3 >]                  │
└─────────────────────────────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.batchReference` | Current batch reference |
| `vm.batch.Detail.BatchStatus` | Batch status |
| `vm.currentBatchStatus` | Processing status message |
| `vm.isBatchBusy` | Is batch processing |
| `vm.showErrorsOnly` | Filter to errors |

**Bulk Actions:**
| Method | Condition |
|--------|-----------|
| `vm.shipBatch(vm.batch)` | `batch.CanShipAll()` |
| `vm.printBatch(vm.batch)` | `batch.CanPrintAll()` |
| `vm.voidBatch(vm.batch)` | `batch.CanVoidAll()` |
| `vm.deleteBatch(vm.batch)` | `batch.CanDeleteAll()` |

---

### 6. `createbatchTemplate.html` (106 lines)

**Purpose:** Create a Batch — manually build a batch by entering order numbers one at a time.

**Screen Layout:**
```
┌─────────────────────────────────────────┐
│  Create a Batch                          │
│                                         │
│  ○ New Batch    ○ Existing Batches      │
│                                         │
│  Batch Reference: [____________]        │
│  Ship Date: [__________] 📅            │
│                                         │
│  Order Number: [____________] [Add]     │
│                                         │
│  Batch Items:                           │
│  ┌─────────────────────────────────┐    │
│  │ Item1 | Item2 | Item3 | ...    │    │
│  └─────────────────────────────────┘    │
│                              [Submit]    │
└─────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.batchDiv` | 'N' (new) or 'E' (existing) |
| `vm.batchReference.BatchReference` | Batch name |
| `vm.shipDate` | Ship date |
| `vm.batchItemReference` | Current item being added |
| `vm.batchItems` | Array of added items |
| `vm.batches` | Existing batches (when mode = 'E') |

**Key Actions:**
| Method | Purpose |
|--------|---------|
| `vm.addBatchItem(ref)` | Add an item to the batch (Enter key) |
| `vm.validateBatch()` | Validate batch reference is unique |
| `vm.getBatches()` | Load existing batches |
| `vm.getBatchItems(ref)` | Load items for existing batch |

---

### 7. `historySearchTemplate.html` (173 lines)

**Purpose:** History search filter panel — appears on the left side of the History page. Provides extensive search criteria for finding shipped packages.

**Available Search Fields:**
| Field | VM Property | Notes |
|-------|-------------|-------|
| Site | `vm.site` | Hidden if user has single site |
| Shipper | `vm.selectedShipper` | Dropdown |
| Carrier | `vm.selectedCarrier` | Dropdown |
| Service | `vm.selectedService` | Dropdown |
| Global MSN | `vm.msn` | Text |
| Global Bundle ID | `vm.bundleId` | Text |
| Ship ID | `vm.shipId` | Text |
| Tracking Number | `vm.trackingNumber` | Text |
| Batch Reference | `vm.batchReference` | Text |
| Batch Item Reference | `vm.batchItemReference` | Text |
| Shipper Reference | `vm.shipperReference` | Text + Exact Match checkbox |
| Consignee Reference | `vm.consigneeReference` | Text + Exact Match checkbox |

**Collapsible "Consignee" Section:**
| Field | VM Property |
|-------|-------------|
| Company Name | `vm.companyName` |
| Contact | `vm.contact` |
| City | `vm.city` |
| State/Province | `vm.state` |
| Country | `vm.country` |
| Postal Code | `vm.postalCode` |
| Phone | `vm.phone` |

**Collapsible "References" Section:**
- `vm.miscRef1` through `vm.miscRef20` (MiscReference1-20)

**Common Customization Points:**
- Hide/show specific search fields
- Add date range fields (ship date from/to)
- Reorder fields
- Change default collapsed/expanded state of sections
- Add custom search fields for company-specific data

**Related Hooks (CBR):**
- `PreSearchHistory(searchCriteria)` — Modify search before execution
- `PostSearchHistory(packages)` — Filter/transform results

---

### 8. `searchresultTemplate.html` (54 lines)

**Purpose:** History search results table — displays the packages found by the history search.

**Table Columns (all conditionally visible):**
| Column | Property | Hidden Flag |
|--------|----------|-------------|
| Actions (View/Void/Reprint) | — | `vm.hideAction` |
| Global MSN | `package.GlobalMsn` | `vm.hideGlobalMSN` |
| Tracking Number | `package.TrackingNumber` | `vm.hideTrackingNumber` |
| Shipper Reference | `package.ShipperReference` | `vm.hideShipperReference` |
| Consignee Reference | `package.ConsigneeReference` | `vm.hideConsigneeReference` |
| Ship Date | `package.Shipdate` | `vm.hideShipDate` |
| Weight | `package.Weight` | `vm.hideWeight` |
| Rated Weight | `package.RatedWeight` | `vm.hideRatedWeight` |
| Dimensions | `package.Dimensions` | `vm.hideDimension` |

**Per-Row Actions:**
| Action | Method | Condition |
|--------|--------|-----------|
| View | `vm.openPopupModal('view', package)` | Always |
| Void | `vm.runVoid(package)` | Not voided, not imported |
| Reprint | `vm.print(package)` | Not voided, not imported |

**Tracking Number Behavior:**
- If `package.IsTrackable && !package.Voided` → clickable link (`vm.track(package)`)
- If voided → strikethrough text

**Common Customization Points:**
- Add/remove columns
- Change column order
- Add custom action buttons
- Change tracking link behavior
- Add conditional row styling

---

### 9. `closeManifestTemplate.html` (26 lines)

**Purpose:** Close Manifest (End of Day) — select carrier/shipper and close manifest dates.

**Screen Layout:**
```
┌─────────────────────────────────────────┐
│  Close Manifest                          │
│                                         │
│  [Shipper/Carrier selector]             │
│  Close Manifest Date: [multi-select]    │
│                                         │
│                        [Close Manifest]  │
└─────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.selectedShipper` | Selected shipper |
| `vm.selectedCarrier` | Selected carrier |
| `vm.ManifestItems` | Available manifest date items |
| `vm.SelectedManifestItems` | Selected items to close |
| `vm.btnDisabled` | Disable button flag |

**Key Actions:**
| Method | Purpose |
|--------|---------|
| `vm.closeManifestDateSelected()` | When manifest items are selected |
| `vm.closeManifest()` | Execute the close manifest |

**Uses `<shipper-carrier>` directive** for shipper/carrier selection.

---

### 10. `transmitTemplate.html` (26 lines)

**Purpose:** Transmit — send shipment data electronically to carrier.

**Screen Layout:**
```
┌─────────────────────────────────────────┐
│  Transmit                                │
│                                         │
│  [Shipper/Carrier selector]             │
│  Transmit Items: [multi-select]         │
│                                         │
│                              [Transmit]  │
└─────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.selectedShipper` | Selected shipper |
| `vm.selectedCarrier` | Selected carrier |
| `vm.transmitItems` | Available transmit items |
| `vm.selectedTransmitItem` | Selected items to transmit |
| `vm.btnDisabled` | Disable button flag |

**Key Actions:**
| Method | Purpose |
|--------|---------|
| `vm.transmitItemSelected()` | When transmit items are selected |
| `vm.transmit()` | Execute the transmission |

---

### 11. `manifestDocumentsTemplate.html` (39 lines)

**Purpose:** Manifest Documents — reprint manifest/end-of-day documents.

**Screen Layout:**
```
┌─────────────────────────────────────────┐
│  Manifest Documents                      │
│                                         │
│  [Shipper/Carrier selector]             │
│  History Items: [select list]           │
│                                         │
│  Manifest Documents: [select list]      │
│                                         │
│                        [Select Printer]  │
└─────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.manifestFiles` | Available history items |
| `vm.selectedHistoryItem` | Selected history item |
| `vm.manifestDocuments` | Available manifest documents |
| `vm.selectedManifestDocument` | Selected document to print |

---

### 12. `managedataTemplate.html` (58 lines)

**Purpose:** Manage Data — purge/reprocess history and transmit items.

**Screen Layout:**
```
┌─────────────────────────────────────────┐
│  Manage Data                             │
│                                         │
│  Manage: ○ History Items ○ Transmit     │
│                                         │
│  [Shipper/Carrier selector]             │
│  Items: [multi-select list]             │
│                                         │
│  Days to Maintain: [===slider===] [180] │
│                                [Filter]  │
│                                         │
│        [Reprocess]  [Delete]            │
└─────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.manageItemText` | Current mode ("History Items" or "Transmit Items") |
| `vm.manageItems` | Available items |
| `vm.selectedManageItems` | Selected items |
| `vm.daysToMaintain` | Retention period (0-180 slider) |

**Key Actions:**
| Method | Purpose |
|--------|---------|
| `vm.getManageData()` | Load items for selected mode |
| `vm.manageItemsSelected()` | When items are selected |
| `vm.filterManageItems()` | Apply days filter |

---

### 13. `addressbookTemplate.html` (155 lines)

**Purpose:** Address Book Manager — full CRUD for address books and entries.

**Screen Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│  Address Books                                   [Add Address Book] │
├──────────────────┬──────────────────────────────────────────────┤
│  ☐ Select All    │  Search/Filter entries                       │
│  ┌────────────┐  │  ┌──────────────────────────────────────┐    │
│  │ Book1 User │  │  │ Table of address book entries         │    │
│  │ Book2 Site │  │  │ Code | Company | Contact | ...       │    │
│  │ Book3 Co.  │  │  │ [Select] action per row              │    │
│  └────────────┘  │  └──────────────────────────────────────┘    │
│  Per-book:       │                                              │
│  [Upload][Export] │  [Add Entry] [Edit] [Delete]               │
│  [Edit][Delete]  │                                              │
└──────────────────┴──────────────────────────────────────────────┘
```

**Key Features:**
- Multi-select address books (checkbox per book)
- Address books have scope: User, Site, or Company level
- Per-book actions: Upload CSV, Export, Edit name, Delete
- Entry search with full address criteria
- Pagination of entries

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.addressBooks` | Available address books |
| `vm.addressBook` | Selected address book IDs (array) |
| `vm.isSelectAllCheckboxChecked` | Select all flag |

**Related Hooks (CBR):**
- `PostSelectAddressBook(shipmentRequest, nameaddress)` — After an address is selected from the book and applied to the shipment

---

### 14. `distributionListTemplate.html` (171 lines)

**Purpose:** Distribution List Manager — manage lists of addresses that receive identical shipments (one scan → multiple packages).

**Screen Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│  Distribution List                                              │
│                                                                 │
│  Select Distribution List: [dropdown▼]  [Add][Edit][Delete]     │
├─────────────────────────────────────────────────────────────────┤
│  Selected: MyDistList                                           │
│  [Add Addresses] [Remove Addresses]                             │
│                                                                 │
│  [Search Criteria panel]  │  [Address entries table]            │
│                           │  Code | Company | City | ...        │
└─────────────────────────────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.distributionLists` | Available distribution lists |
| `vm.distributionList` | Selected distribution list |
| `vm.distributionList.AddressBookEntries` | Entries in selected list |

**Related Hooks (SBR):**
- `LoadDistributionList(value, shipmentRequest, userParams)` — Returns `List<ShipmentRequest>` for all distribution entries

---

### 15. `groupmanagerTemplate.html` (180 lines)

**Purpose:** Group Manager — manage multi-piece shipment groups (e.g., UPS World Ease, TradeDirect, pallets).

**Screen Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│  Group Manager                                                  │
├─────────────────────────────────────────────────────────────────┤
│  Carrier: [dropdown▼]  Group Type: [dropdown▼]                  │
│  Grouping Status: [dropdown▼]           [Create Group]          │
├─────────────────────────────────────────────────────────────────┤
│  Shipper: [dropdown▼]  Ship Date: [___] 📅                     │
│  Doc Box Tracking #: [________]         [Search] [Clear]        │
├─────────────────────────────────────────────────────────────────┤
│  Groups                                         [10][25][50][100]│
│ ┌───────────────────────────────────────────────────────────┐   │
│ │ Name | DocBox TN | Symbol | # Packages | Actions          │   │
│ │ GRP1 | 1Z123    | SYM01  | 5          | [Close][Modify]  │   │
│ └───────────────────────────────────────────────────────────┘   │
│  Showing 1 to 10 of 25 entries    [< 1 2 3 >]                  │
└─────────────────────────────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.carrier` | Selected carrier |
| `vm.groupType` | Selected group type (has `CanCreate`, `CanClose`, `CanOpen`, `CanModify`) |
| `vm.groupStatus` | Selected status filter (Open, Closed) |
| `vm.GroupData` | Array of group objects |
| `vm.shipper` | Selected shipper |
| `vm.shipDate.value` | Ship date filter |
| `vm.docBoxTrackingNumber` | Doc box tracking number filter |
| `vm.enumGroupTypes.TradeDirect` | Special group type identifier |

**Per-Group Actions (conditional):**
| Action | Shows When | Method |
|--------|-----------|--------|
| Close Group | `CanClose && status == Open` | `vm.closeGroupClick(group)` |
| Open Group | `CanOpen && status == Closed` | `vm.openGroupClick(group)` |
| Modify Group | `CanModify && status == Open` | `vm.modifyGroupClick(group)` |
| Get Documents | `status == Closed && documents exist` | `vm.openDocuments(group)` |

**Related Hooks:**
- **CBR:** `PreCreateGroup/PostCreateGroup`, `PreModifyGroup/PostModifyGroup`, `PreCloseGroup/PostCloseGroup`
- **SBR:** Same-named server hooks

---

### 16. `rateResultModal.html` (217 lines)

**Purpose:** Rate Results Modal — displays rate shopping results in a popup after rating.

**Screen Layout:**
```
┌─────────────────────────────────────────────────────────────────┐
│  Rate Results                           Sort Mode: Lowest Rate   │
│  ✕                                                              │
├─────────────────────────────────────────────────────────────────┤
│  Service     | Base    | Discount | Special | Total  | Arrival  │
│  UPS Ground  | $12.50  | -$1.00   | $0.00   | $11.50 | 2024-01-05 │ ▼
│  ┌─ Expanded per-package detail ──────────────────────────┐     │
│  │  Pkg | Weight | Base | Discount | Special | Total      │     │
│  │  1   | 5 lbs  | ...  | ...      | ...     | ...       │     │
│  └────────────────────────────────────────────────────────┘     │
│  UPS Next Day| $45.00  | -$3.00   | $0.00   | $42.00 | 2024-01-03 │ ▶
│  UPS 2nd Day | $28.00  | -$2.00   | $0.00   | $26.00 | 2024-01-04 │ ▶
├─────────────────────────────────────────────────────────────────┤
│  (No services meet criteria message if empty)                   │
└─────────────────────────────────────────────────────────────────┘
```

**Key ViewModel Properties:**
| Property | Purpose |
|----------|---------|
| `vm.rateResults` | Array of `ShipmentResponse` objects (one per service) |
| `vm.sortMode` | Current sort mode name |
| `vm.selectedRow` | Currently highlighted service symbol |
| `vm.expandedRateResult` | Index of expanded row (-1 = none) |
| `vm.title` | Modal title |
| `vm.serviceSelectionMode` | How services are selected |

**Per-Rate-Result Properties:**
| Property | Purpose |
|----------|---------|
| `rateResult.PackageDefaults.Service.Name` | Service name |
| `rateResult.PackageDefaults.Service.Symbol` | Service symbol |
| `rateResult.PackageDefaults.BaseCharge` | Base charge (Amount, Currency) |
| `rateResult.PackageDefaults.Discount` | Discount (Amount, Currency) |
| `rateResult.PackageDefaults.Special` | Special charges (Amount, Currency) |
| `rateResult.PackageDefaults.Total` | Total (Amount, Currency) |
| `rateResult.PackageDefaults.ArriveDate` | Arrival date (Year, Month, Day) |
| `rateResult.PackageDefaults.ArriveTime` | Arrival time string |

**Selecting a Service:**
- Click service name → `vm.onSelectService(rateResult.PackageDefaults.Service, 'ApplicableService')`
- Disabled if `vm.profile.ProfileSetting.DisableServiceChanges`

**Uses `<currency>` directive** for formatting monetary values.

**Related Hooks (CBR):**
- `PostRate(shipmentRequest, rateResults)` — Can modify/filter rate results before modal shows

---

### 17. `mailRoomSearchFiltersTemplate.html` & `mailRoomSearchResultTemplate.html`

**Purpose:** Mail Room specific search — similar to history search but for mail room processing workflow. Only shown when `EnableMailRoomProcessing` is true.

### 18. `pendingShipmentSearchFiltersTemplate.html` & `pendingShipmentSearchResultTemplate.html`

**Purpose:** Pending Shipments — saved/staged shipments that haven't been shipped yet. Shown when `DisablePendingShipment` is false.

---

## Custom Directives Reference

These are reusable AngularJS directives used across templates:

### `<field-select>`
```html
<field-select property="UniquePropertyName"
              model="vm.boundProperty"
              options="item.Value as item.Name for item in vm.collection"
              caption="{{vm.translations.Common.Label}}"
              change-function="vm.onChange()"
              form-type="Horizontal">
</field-select>
```
- `property` — Unique identifier (used by FieldOptions for visibility/caption)
- `model` — Two-way bound value
- `options` — AngularJS `ng-options` expression
- `caption` — Display label
- `change-function` — Called on change
- `form-type` — Layout style ("Horizontal" = label left, control right)

### `<field-input>`
```html
<field-input property="UniquePropertyName"
             model="vm.boundProperty"
             caption="{{vm.translations.Common.Label}}"
             form-type="Horizontal">
</field-input>
```

### `<field-date>`
```html
<field-date property="UniquePropertyName"
            caption="{{vm.translations.Common.ShipDate}}"
            form-type="Horizontal"
            model="vm.dateObject.value"
            is-open="vm.dateObject.opened"
            datepicker-options="vm.dateOptions"
            change-function="vm.onDateChange()"
            open-date-picker-function="vm.openDatePicker()">
</field-date>
```

### `<field-checkbox>`
```html
<field-checkbox property="UniquePropertyName"
                model="vm.booleanProperty"
                caption="Label Text"
                change="vm.onCheck()">
</field-checkbox>
```

### `<name-address>`
```html
<name-address caption="Consignee"
              nameaddress="vm.currentShipment.PackageDefaults.Consignee"
              countries="vm.profile.Countries"
              translations="vm.translations"
              field-options="vm.profile.FieldOptions"
              address-property-name="Consignee"
              current-shipment="vm.currentShipment"
              hide-address-book-list="false"
              hide-checkboxes="false"
              hide-validate="false"
              hide-search="false"
              active-subtab-index="1"
              is-update-address-book-entry="vm.isUpdateAddressBookEntry"
              address-book-id="vm.addressBookId"
              disable-Fields="false">
</name-address>
```
The `<name-address>` directive renders a full address entry form with:
- Company, Contact, Address1-3, City, State, Postal Code, Country, Phone, Email
- Residential/PO Box checkboxes
- Address book search/select
- Address validation button
- Custom data fields

### `<shipper-carrier>`
```html
<shipper-carrier selected-shipper="vm.selectedShipper"
                 selected-carrier="vm.selectedCarrier">
</shipper-carrier>
```
Renders linked shipper and carrier dropdowns.

### `<keyboard-event>`
```html
<keyboard-event key="F12" on-keypress="vm.checkShip()" ng-disabled="vm.disableCondition"></keyboard-event>
```

### `<currency>`
```html
<currency currency_value="vm.amount" currency_symbol="vm.currencyCode"></currency>
```

---

## Field Visibility System

Every field in the UI can be hidden/shown and have its caption overridden via `vm.profile.FieldOptions`. The `property` attribute on directives maps to this system.

```javascript
// Example FieldOptions structure:
vm.profile.FieldOptions = {
    Load: { IsHidden: false, Caption: "Scan Order" },
    Consignee: { IsHidden: false },
    ReturnAddress: { IsHidden: true },
    General: { IsHidden: false },
    Accessorials: { IsHidden: true },
    // ... etc
}
```

**How it works in templates:**
- `ng-hide="{{vm.profile.FieldOptions.Load.IsHidden}}"` — Hides the element
- `vm.loadButtonCaption` — Caption from FieldOptions (resolved in controller)

**Profile Settings controlling major features:**
| Setting | Effect |
|---------|--------|
| `DisableShip` | Hides ship button |
| `DisableLoadFromBusinessRule` | Disables custom load |
| `DisableLoadFromFile` | Disables file batch load |
| `DisablePendingShipment` | Hides pending shipments |
| `DisableServiceChanges` | Prevents changing service from rate modal |
| `EnableMailRoomProcessing` | Shows mail room button |
| `EnableLicensePlate` | Shows license plate button |
| `EnableLoadFromMailRoom` | Adds mail room load option |
| `EnableLoadFromBatch` | Adds batch load option |
| `ServiceSelectionMode` | Controls rate/service workflow |

---

## CBR Interaction with Templates

The CBR JavaScript can interact with the template through the `this.vm` ViewModel. Here's how:

### Accessing and Modifying Data
```javascript
// Read current shipment data
var weight = this.vm.currentShipment.Packages[this.vm.packageIndex].Weight;

// Set a field value (template automatically updates via binding)
this.vm.currentShipment.PackageDefaults.Consignee.Company = "ACME Corp";

// Access profile data
var shippers = this.vm.profile.Shippers;
var userInfo = this.vm.profile.UserInformation;
```

### Triggering Actions
```javascript
// These methods are available on vm and trigger the same flow as button clicks:
// vm.load(), vm.newShipment(), vm.rate(), vm.checkShip(), etc.
```

### Showing/Hiding Elements via DOM (when template customization isn't possible)
```javascript
this.PageLoaded = function(location) {
    if (location === '/shipping') {
        // jQuery-based manipulation (last resort)
        $('[property="SomeField"]').closest('.form-group').hide();
    }
}
```

### Common CBR + Template Patterns

**Pattern 1: Auto-fill field after load**
```javascript
this.PostLoad = function(loadValue, shipmentRequest) {
    // Set a reference field that the template binds to
    shipmentRequest.Packages[0].ShipperReference = loadValue;
}
```

**Pattern 2: Validate before ship**
```javascript
this.PreShip = function(shipmentRequest, userParams) {
    if (!shipmentRequest.Packages[0].Weight || shipmentRequest.Packages[0].Weight.Amount <= 0) {
        client.alert.Danger("Weight is required!");
        throw "Weight validation failed";
    }
}
```

**Pattern 3: Custom keyboard shortcut (add to template)**
```html
<keyboard-event key="F5" on-keypress="vm.customAction()"></keyboard-event>
```
Then handle in CBR:
```javascript
this.Keystroke = function(shipmentRequest, vm, event) {
    if (event.key === 'F5') {
        // Custom logic
    }
}
```

---

## How to Customize Templates

### Adding a New Field
1. Identify the correct template file
2. Find the insertion point in the layout
3. Use the appropriate directive (`<field-input>`, `<field-select>`, etc.)
4. Bind to the correct VM property
5. Set a unique `property` name

**Example — Add a "Department" field after the Load field:**
```html
<field-input property="CustomDepartment"
             model="vm.currentShipment.Packages[vm.packageIndex].UserData1"
             caption="Department"
             form-type="Horizontal">
</field-input>
```

### Hiding an Existing Field
**Option A (template):** Add `ng-hide="true"` or wrap in `ng-if="false"`
**Option B (profile):** Set via FieldOptions in profile (no template change needed)
**Option C (CBR):** Use DOM manipulation in `PageLoaded`

### Reordering Fields
Move the HTML block to the desired position within the template.

### Adding a Custom Button
```html
<button class="btn btn-xs btn-primary" ng-click="vm.customMethod()">Custom Action</button>
```
Then implement `vm.customMethod` logic in CBR or via `UserMethod` API call.

### Changing Layout
Modify Bootstrap grid classes (`col-md-*`) to adjust widths. The shipping template uses a `col-md-4` / `col-md-8` split by default.

---

## Response Format

When given a blueprint document, respond with:

1. **Template Analysis** — Which template file(s) need changes and why
2. **Modified Template Code** — The complete modified HTML for each affected template (or just the changed sections with context)
3. **CBR Code** — Any JavaScript needed for the `ClientBusinessRules` function
4. **SBR Code** — Any C# server-side hooks needed (reference `shipexec-hooks.md` for details)
5. **Field Options** — Any FieldOptions/ProfileSettings that need configuration
6. **Deployment Notes** — Which files to deploy and where

### Template Modification Rules:
- Preserve all existing `ng-model` bindings unless explicitly changing them
- Keep `property` attributes unique across the template
- Maintain Bootstrap grid structure (rows must contain columns that sum to 12)
- Use `vm.translations.*` for text that should be translatable
- Use `vm.profile.FieldOptions.*.IsHidden` for visibility when possible
- Don't break existing keyboard shortcuts unless the blueprint requires it
- Test conditions: ensure `ng-show`/`ng-hide` logic doesn't conflict with existing conditions
