# PSI.Sox Type Definitions Reference Prompt

## Purpose

This document provides detailed type definitions for all PSI.Sox classes, sub-classes, and enums used in ShipExec Server Business Rules (SBR) code. Use this as authoritative reference when generating, repairing, or reviewing SBR C# code. **Do NOT guess property types — consult this document.**

> **IMPORTANT**: Do NOT add `PSI.Sox.Interfaces` to using statements in output files; it does not exist as a user-facing namespace in compiled output. The interfaces below are implemented internally by the ShipExec runtime.

---

## Core Request/Response Classes

### ShipmentRequest

The primary input object for Load, Ship, Rate, and Batch operations. Represents a complete shipment with defaults and one or more packages.

```csharp
namespace PSI.Sox
{
    public class ShipmentRequest
    {
        /// <summary>Shipment-level defaults applied to all packages unless overridden at package level.</summary>
        public Package PackageDefaults { get; set; }

        /// <summary>List of individual packages in this shipment. For single-piece, use index 0.</summary>
        public List<PackageRequest> Packages { get; set; }
    }
}
```

**Key usage patterns:**
- `shipmentRequest.PackageDefaults.Consignee` → sets the ship-to address for all packages
- `shipmentRequest.PackageDefaults.Service` → sets the carrier service (type is `Service`, NOT a string)
- `shipmentRequest.PackageDefaults.Terms` → billing terms (type is `string`, e.g., "SHIPPER", "DDU", "COLLECT")
- `shipmentRequest.PackageDefaults.Shipper` → shipper symbol (type is `string`)
- `shipmentRequest.PackageDefaults.Shipdate` → ship date (type is `Date`, NOT `DateTime`)
- `shipmentRequest.PackageDefaults.ThirdPartyBilling` → bool flag
- `shipmentRequest.PackageDefaults.ThirdPartyBillingAddress` → `NameAddress` object
- `shipmentRequest.PackageDefaults.ReturnAddress` → `NameAddress` (ship-from / return address)
- `shipmentRequest.PackageDefaults.ImporterOfRecord` → `NameAddress`
- `shipmentRequest.PackageDefaults.ExportDeclarationStatement` → `string`
- `shipmentRequest.PackageDefaults.ShipperReference` → `string`
- `shipmentRequest.PackageDefaults.ConsigneeReference` → `string`
- `shipmentRequest.PackageDefaults.MiscReference1` through `MiscReference5` → `string`
- `shipmentRequest.PackageDefaults.UserData1` through `UserData5` → `string`
- `shipmentRequest.Packages[0].Weight` → `Weight` object
- `shipmentRequest.Packages[0].Dimensions` → `Dimensions` object
- `shipmentRequest.Packages[0].CommodityContents` → `List<CommodityContent>`

---

### Package (used as PackageDefaults)

The `Package` class represents shipment-level default values. It is used as `ShipmentRequest.PackageDefaults` and also appears in `ShipmentResponse.PackageDefaults` and history queries.

```csharp
namespace PSI.Sox
{
    public class Package
    {
        // Address fields
        public NameAddress Consignee { get; set; }
        public NameAddress ReturnAddress { get; set; }
        public NameAddress ThirdPartyBillingAddress { get; set; }
        public NameAddress ImporterOfRecord { get; set; }

        // Service & carrier
        public Service Service { get; set; }            // NOT a string — it's a Service object
        public string Shipper { get; set; }             // Shipper symbol string, e.g. "TEX"
        public string Terms { get; set; }               // "SHIPPER", "COLLECT", "DDU", "THIRD_PARTY"

        // Dates
        public Date Shipdate { get; set; }              // PSI.Sox.Date, NOT System.DateTime

        // Billing
        public bool ThirdPartyBilling { get; set; }

        // References (shipment-level)
        public string ShipperReference { get; set; }
        public string ConsigneeReference { get; set; }
        public string MiscReference1 { get; set; }
        public string MiscReference2 { get; set; }
        public string MiscReference3 { get; set; }
        public string MiscReference4 { get; set; }
        public string MiscReference5 { get; set; }

        // User data (shipment-level)
        public string UserData1 { get; set; }
        public string UserData2 { get; set; }
        public string UserData3 { get; set; }
        public string UserData4 { get; set; }
        public string UserData5 { get; set; }

        // International
        public string ExportDeclarationStatement { get; set; }

        // Response fields (populated after ship/rate)
        public int ErrorCode { get; set; }              // 0 = success
        public string ErrorMessage { get; set; }

        // History/tracking
        public bool Hazmat { get; set; }
        public bool Voided { get; set; }

        // Documents (response only)
        public List<Document> Documents { get; set; }
    }
}
```

---

### PackageRequest

Represents a single package within a shipment. Used in `ShipmentRequest.Packages[]`.

```csharp
namespace PSI.Sox
{
    public class PackageRequest
    {
        // Physical attributes
        public Weight Weight { get; set; }
        public Dimensions Dimensions { get; set; }
        public string Packaging { get; set; }           // e.g. "CUSTOM", "UPS_LETTER"

        // References (package-level)
        public string ShipperReference { get; set; }
        public string ConsigneeReference { get; set; }
        public string MiscReference1 { get; set; }
        public string MiscReference2 { get; set; }
        public string MiscReference3 { get; set; }
        public string MiscReference4 { get; set; }
        public string MiscReference5 { get; set; }

        // User data (package-level)
        public string UserData1 { get; set; }
        public string UserData2 { get; set; }
        public string UserData3 { get; set; }
        public string UserData4 { get; set; }
        public string UserData5 { get; set; }

        // Accessorials
        public bool Proof { get; set; }                          // Delivery confirmation
        public bool ProofRequireSignature { get; set; }          // Signature required
        public bool SaturdayDelivery { get; set; }
        public bool DocumentsOnly { get; set; }
        public Money DeclaredValueAmount { get; set; }           // type is Money
        public string Description { get; set; }
        public string ExportReason { get; set; }                 // e.g. "Sale"

        // International / Commodities
        public List<CommodityContent> CommodityContents { get; set; }
        public int CommercialInvoiceMethod { get; set; }         // 0 = paper, 1 = paperless
        public NameAddress ImporterOfRecord { get; set; }

        // Email notification
        public bool ShipNotificationEmail { get; set; }
        public string ShipNotificationAddressEmail { get; set; }
    }
}
```

---

### ShipmentResponse

Returned after Ship and Rate operations. Contains results for each package.

```csharp
namespace PSI.Sox
{
    public class ShipmentResponse
    {
        /// <summary>Response-level defaults/summary (error info, etc.)</summary>
        public Package PackageDefaults { get; set; }

        /// <summary>Per-package results</summary>
        public List<PackageResponse> Packages { get; set; }
    }
}
```

**Key usage:**
- `shipmentResponse.PackageDefaults.ErrorCode` → `int` (0 = success)
- `shipmentResponse.PackageDefaults.ErrorMessage` → `string`
- `shipmentResponse.Packages[i].ErrorCode` → `int`
- `shipmentResponse.Packages[i].ErrorMessage` → `string`
- `shipmentResponse.Packages[i].Documents` → `List<Document>`
- `shipmentResponse.Packages[i].TrackingNumber` → `string`

---

### PackageResponse

Result for a single package after ship/rate.

```csharp
namespace PSI.Sox
{
    public class PackageResponse
    {
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string TrackingNumber { get; set; }
        public Money ApportionedTotal { get; set; }     // Rated cost
        public List<Document> Documents { get; set; }
    }
}
```

---

## Address & Contact Classes

### NameAddress

Used for Consignee, ReturnAddress, ThirdPartyBillingAddress, ImporterOfRecord, and address validation.

```csharp
namespace PSI.Sox
{
    public class NameAddress
    {
        public string Company { get; set; }
        public string Contact { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }             // Country symbol, e.g. "US", "UNITED_STATES", "CA"
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Account { get; set; }             // Carrier account number (for 3rd party/importer)
        public bool Residential { get; set; }
        public bool PoBox { get; set; }
        public List<CustomDataItem> CustomData { get; set; }
    }
}
```

### NameAddressValidationCandidate

Returned from AddressValidation operations.

```csharp
namespace PSI.Sox
{
    public class NameAddressValidationCandidate
    {
        public NameAddress Address { get; set; }
        public string MatchLevel { get; set; }
    }
}
```

---

## Service & Carrier Classes

### Service

Represents a carrier service. **This is an object, NOT a string.** Set `Service.Symbol` to assign a service.

```csharp
namespace PSI.Sox
{
    public class Service
    {
        public string Symbol { get; set; }              // e.g. "CONNECTSHIP_UPS.UPS.GND", "PSI.UPS.GND"
        public string Name { get; set; }                // Display name
        public string Carrier { get; set; }             // Carrier portion, e.g. "CONNECTSHIP_UPS.UPS"
    }
}
```

**Common service symbol patterns:**
- `CONNECTSHIP_UPS.UPS.GND` – UPS Ground
- `CONNECTSHIP_UPS.UPS.NDA` – UPS Next Day Air
- `CONNECTSHIP_UPS.UPS.2DA` – UPS 2nd Day Air
- `CONNECTSHIP_UPS.UPS.3DA` – UPS 3 Day Select
- `CONNECTSHIP_UPS.UPS.EXP` – UPS Worldwide Express
- `CONNECTSHIP_FEDEX.FEDEX.GND` – FedEx Ground
- `CONNECTSHIP_FEDEX.FEDEX.PRI` – FedEx Priority Overnight

---

## Measurement Classes

### Weight

```csharp
namespace PSI.Sox
{
    public class Weight
    {
        public double Amount { get; set; }              // Numeric weight value
        public string Units { get; set; }               // "LB" or "KG"
    }
}
```

### Money

```csharp
namespace PSI.Sox
{
    public class Money
    {
        public double Amount { get; set; }              // Numeric value
        public string Currency { get; set; }            // "USD", "CAD", "EUR", etc.
    }
}
```

### Dimensions

```csharp
namespace PSI.Sox
{
    public class Dimensions
    {
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
```

### Date

ShipExec's date wrapper. **Use this instead of `System.DateTime` for shipment dates.**

```csharp
namespace PSI.Sox
{
    public class Date
    {
        public Date(DateTime dateTime) { }
        // Wraps a DateTime; used for Shipdate property
    }
}
```

---

## International / Commodity Classes

### CommodityContent

Line item for international shipments. Added to `PackageRequest.CommodityContents`.

```csharp
namespace PSI.Sox
{
    public class CommodityContent
    {
        public string ProductCode { get; set; }         // Harmonized/tariff code or SKU
        public string Description { get; set; }         // Item description
        public string OriginCountry { get; set; }       // Country of manufacture
        public long Quantity { get; set; }              // Number of units (type is long, NOT int)
        public string QuantityUnitMeasure { get; set; } // "PCS", "EA", "DOZ", etc.
        public Money UnitValue { get; set; }            // Per-unit monetary value
        public Weight UnitWeight { get; set; }          // Per-unit weight
    }
}
```

### HazmatContent

Hazardous materials line item.

```csharp
namespace PSI.Sox
{
    public class HazmatContent
    {
        // Hazmat-specific properties (UN number, class, packing group, etc.)
    }
}
```

---

## Batch Classes

### BatchRequest

Container for batch processing. Returned from `LoadBatch` and `ParseBatchFile`.

```csharp
namespace PSI.Sox
{
    public class BatchRequest
    {
        public string BatchReference { get; set; }      // Unique batch identifier
        public List<BatchItem> BatchItems { get; set; } // Individual items to ship
    }
}
```

### BatchItem

A single item within a batch.

```csharp
namespace PSI.Sox
{
    public class BatchItem
    {
        public string BatchItemReference { get; set; }  // Unique item reference
        public int SequenceNumber { get; set; }         // Order within the batch
        public ShipmentRequest ShipmentRequest { get; set; } // Full shipment data for this item
    }
}
```

### BatchReference

Used to populate the batch selection list in the UI.

```csharp
namespace PSI.Sox
{
    public class BatchReference
    {
        public string Reference { get; set; }
        public string Description { get; set; }
    }
}
```

### Batch

Represents a stored batch in the system.

```csharp
namespace PSI.Sox
{
    public class Batch
    {
        public string BatchReference { get; set; }
        // Additional metadata fields
    }
}
```

### ProcessBatchActions

Configuration for how a batch should be processed.

```csharp
namespace PSI.Sox
{
    public class ProcessBatchActions
    {
        // Flags controlling batch behavior (ship, rate, print, etc.)
    }
}
```

### ProcessBatchResult

Result of batch processing.

```csharp
namespace PSI.Sox
{
    public class ProcessBatchResult
    {
        // Per-item results, success/failure counts
    }
}
```

---

## Document & Print Classes

### DocumentRequest

Passed to Print hooks. Contains the document to be printed.

```csharp
namespace PSI.Sox
{
    public class DocumentRequest
    {
        public DocumentMapping DocumentMapping { get; set; }
    }
}
```

### DocumentMapping

```csharp
namespace PSI.Sox
{
    public class DocumentMapping
    {
        public Document Document { get; set; }
    }
}
```

### Document

Represents a shipping document (label, commercial invoice, etc.).

```csharp
namespace PSI.Sox
{
    public class Document
    {
        public string Symbol { get; set; }              // e.g. "TANDATA_COMMERCIAL_INVOICE.STANDARD"
        public string DocumentSymbol { get; set; }      // Alternate accessor for the symbol
    }
}
```

### DocumentResponse

Result from a print operation.

```csharp
namespace PSI.Sox
{
    public class DocumentResponse
    {
        // Print result info
    }
}
```

### PrinterMapping

Printer configuration.

```csharp
namespace PSI.Sox
{
    public class PrinterMapping
    {
        // Printer name, type, settings
    }
}
```

---

## Manifest & Transmit Classes

### ManifestItem

Used in CloseManifest operations.

```csharp
namespace PSI.Sox
{
    public class ManifestItem
    {
        // Manifest item symbol, e.g. "SHIPDATE_20070308"
    }
}
```

### CloseManifestResult

Result from closing a manifest.

```csharp
namespace PSI.Sox
{
    public class CloseManifestResult
    {
        // Close-out result data
    }
}
```

### TransmitItem

Used in Transmit operations.

```csharp
namespace PSI.Sox
{
    public class TransmitItem
    {
        // Transmit file symbolic name, e.g. "DOM_2_20000310_1_1"
    }
}
```

### TransmitItemResult

Result from transmitting a file.

```csharp
namespace PSI.Sox
{
    public class TransmitItemResult
    {
        // Transmit result per item
    }
}
```

---

## Group Classes

### Group

Represents a package grouping (e.g., multi-piece shipment group).

```csharp
namespace PSI.Sox
{
    public class Group
    {
        // Group metadata, ID, type
    }
}
```

---

## Reprocess & Modify Classes

### ReProcessResult

Result from reprocessing packages.

```csharp
namespace PSI.Sox
{
    public class ReProcessResult
    {
        // Reprocess outcome
    }
}
```

### ModifyPackageListResult

Result from modifying packages in history.

```csharp
namespace PSI.Sox
{
    public class ModifyPackageListResult
    {
        // Modification results
    }
}
```

---

## Packing Classes (PSI.Sox.Packing namespace)

### PackingRequest

```csharp
namespace PSI.Sox.Packing
{
    public class PackingRequest
    {
        // Packing operation input
    }
}
```

### PackingResponse

```csharp
namespace PSI.Sox.Packing
{
    public class PackingResponse
    {
        // Packing operation result
    }
}
```

### PackingRateRequest

```csharp
namespace PSI.Sox.Packing
{
    public class PackingRateRequest
    {
        // Pack rate input
    }
}
```

### PackingRateResponse

```csharp
namespace PSI.Sox.Packing
{
    public class PackingRateResponse
    {
        // Pack rate result
    }
}
```

---

## Configuration & Context Classes

### BusinessRuleSetting

Key/value configuration set in ShipExec Management Studio (Commander). Accessed via `BusinessRuleSettings` property.

```csharp
namespace PSI.Sox
{
    public class BusinessRuleSetting
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
```

**Usage:** Connection strings, file paths, feature flags, and other deployment-specific values.

### ClientContext

Provides information about the current user session.

```csharp
namespace PSI.Sox
{
    public class ClientContext
    {
        public UserInfo User { get; set; }

        /// <summary>Returns user context for API calls</summary>
        public object GetUserContext() { }
    }
}
```

### UserInfo

```csharp
namespace PSI.Sox
{
    public class UserInfo
    {
        public string UserName { get; set; }            // Typically an email address
    }
}
```

### ClientProfile

Returned from `BusinessObjectApi.GetClientProfile()`.

```csharp
namespace PSI.Sox
{
    public class ClientProfile
    {
        public UserInformation UserInformation { get; set; }
    }
}
```

### UserInformation

```csharp
namespace PSI.Sox
{
    public class UserInformation
    {
        public NameAddress Address { get; set; }         // User's address with CustomData
    }
}
```

### CustomDataItem

Key/value pair for custom data on addresses/users.

```csharp
namespace PSI.Sox
{
    public class CustomDataItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
```

---

## SerializableDictionary

A key/value collection used as `userParams` throughout the SBR. **This is NOT a standard `Dictionary<string,string>`** — it is a custom serializable dictionary class.

```csharp
namespace PSI.Sox
{
    public class SerializableDictionary
    {
        // Key/value pair collection
        // Used for passing custom parameters between hooks and between CBR/SBR
    }
}
```

---

## Profile Classes

### IProfile

Available as `Profile` property on `SoxBusinessRules`. Provides access to system configuration.

```csharp
namespace PSI.Sox.Interfaces
{
    public interface IProfile
    {
        List<Shipper> Shippers { get; }
        List<Country> Countries { get; }
    }
}
```

### Shipper

```csharp
namespace PSI.Sox
{
    public class Shipper
    {
        public string Name { get; set; }                // Shipper symbol
        public string Country { get; set; }             // Shipper's country
    }
}
```

### Country

```csharp
namespace PSI.Sox
{
    public class Country
    {
        public string Symbol { get; set; }              // e.g. "UNITED_STATES"
        public string Iso2 { get; set; }                // e.g. "US"
        public string Iso3 { get; set; }                // e.g. "USA"
    }
}
```

### BoxType

```csharp
namespace PSI.Sox
{
    public class BoxType
    {
        // Box name, dimensions, carrier-specific identifiers
    }
}
```

---

## Search & History Classes

### Data.SearchCriteria

Used for querying package history.

```csharp
namespace PSI.Sox.Data
{
    public class SearchCriteria
    {
        public int Take { get; set; }                           // Max results
        public List<WhereClause> WhereClauses { get; set; }
        public List<OrderByClause> OrderByClauses { get; set; }
    }
}
```

### Data.WhereClause

```csharp
namespace PSI.Sox.Data
{
    public class WhereClause
    {
        public string FieldName { get; set; }           // e.g. "Shipdate", "Voided", "ShipperReference"
        public object FieldValue { get; set; }
        public SearchOperator Operator { get; set; }
    }
}
```

### Data.OrderByClause

```csharp
namespace PSI.Sox.Data
{
    public class OrderByClause
    {
        public string FieldName { get; set; }
        public string Direction { get; set; }           // "asc" or "desc"
    }
}
```

### Data.SearchOperator (Enum)

```csharp
namespace PSI.Sox.Data
{
    public enum SearchOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith
    }
}
```

### HistoryPackage

Returned from history search queries.

```csharp
namespace PSI.Sox
{
    public class HistoryPackage
    {
        public long GlobalMsn { get; set; }
        public string TrackingNumber { get; set; }
        public string ShipperReference { get; set; }
        public bool Voided { get; set; }
        public DateTime Shipdate { get; set; }
        // Additional history fields
    }
}
```

---

## Pickup Class

```csharp
namespace PSI.Sox
{
    public class Pickup
    {
        // Pickup scheduling information for Ship operations
    }
}
```

---

## Enums

> **Complete list of all enums in PSI.Sox assemblies.** Use these exact values when working with enum-typed properties.

### PSI.Sox Namespace

```csharp
public enum BatchStatus { Open, Closed, ClosedLocked }
public enum CarrierNameType { Ups, UpsFreight }
public enum DatabaseType { Sox, App, Custom, Adapter, Config }
public enum DeviceParserType { Default, Custom, Fairbanks, Toledo, CubiScan, ExpressCube }
public enum DocumentType { Package, Bundle, PackageList, Container, Group, HistoryItem }
public enum FuelSurchargeType { Ground, Air, International, TransBorderGround }
public enum ImageType { None, Png, Bmp, Tiff, Gif, Jpeg }
public enum LogLevel { Trace, Debug, Info, Warning, Error, Fatal }
public enum PortType { Serial, Usb, Network }
public enum PrintActions { Direct, Raw, Xml, Image, Pdf }
public enum PrintDirection { BottomFirst, TopFirst }
public enum SearchCloseoutStatus { All, Open, Closed }
public enum SortType { NoOrder, LowestRate, EarliestCommitment }
public enum SsoProtocol { None, OpenIdConnect, SAML }
public enum SummaryDataSortParameter { SiteId, ShipperReference, Shipper, ConsigneeReference }
public enum TaxIdType { Undefined, Individual, Business, Passport, Military }
public enum TemplateTypes { BatchManager, BatchDetails, CloseManifest, CreateBatch, GroupManager, HistorySearchFilter, ManifestDocuments, ScanShip, Shipping, Transmit, ManageData, AddressBook, HistorySearchResult, DistributionList, LoadBatch, RateResult, PendingShipment, PendingShipmentSearchResult, Mailroom, MailroomSearchResult, HistoryTemplate }
public enum TernarySetting { NotSet, True, False }
public enum TransactionType { Ship, Rate, Track, Void, Closeout, Transmit, Purge, Pickup }
public enum TransponderFrequency { Unknown, HighFrequency, UltraHighFrequency }
public enum TransponderType { Unknown, EPCClass0, EPCClass1, EPCClass1Gen2 }
public enum UpsPremierType { None, Silver, Gold, Platinum }
public enum UserRegistrationStatus { Pending, Approved, Reject }
public enum ValidationType { None, RegularExpression, AlphaNumericOnly, NumericOnly, ValidationList, ValidationListDropDown, Email, MultipleEmail }
public enum VendorCollectIdTypeCode { IOSS, VOEC, HMRC }
public enum WeekDays { Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday }
```

### PSI.Sox.SoxErrorCode (Hook/Operation Identifiers)

```csharp
public enum SoxErrorCode
{
    BrGetBoxTypes, BrUserNameValidate, BrUserMethod, BrLoad, BrLoadDistributionList,
    BrLoadBatch, BrPreRate, BrRate, BrPostRate, BrPreShip, BrShip, BrPostShip,
    BrPrePrint, BrPrint, BrPostPrint, BrErrorLabel, BrPreVoid, BrVoid, BrPostVoid,
    BrPreCloseOut, BrCloseManifest, BrPostCloseOut, BrPreTransmit, BrTransmit,
    BrPostTransmit, BrPreProcessBatch, BrPostProcessBatch, BrParseBatchFile,
    BrPrePack, BrPostPack, BrPrePackRate, BrPostPackRate, BrClientNewShipment,
    BrPreAddressValidation, BrAddressValidation, BrClientKeystroke, BrClientPageLoad,
    BrPostAddressValidation, BrPreReProcess, BrPostReProcess, BrClientAddPackage,
    BrPreModifyPackageList, BrPostModifyPackageList, BrClientPreviousPackage,
    BrClientNextPackage, BrPreCloseGroup, BrPostCloseGroup, BrPreCreateGroup,
    BrClientPreLoad, BrPostCreateGroup, BrPreModifyGroup, BeClientPostLoad,
    BrPostModifyGroup, BrGetBatchReferences, BrGetCommodityContents, BrCloseGroup,
    BrClientPreShip, BrClientPostShip, BrClientPrePrint, BrClientPostPrint,
    BrClientPreCloseOut, BrClientPostCloseOut,
    // Operation codes
    Load, LoadBatch, LoadStagedBatch, StageBatch, AddShipmentToBatch, ParseBatchFile,
    Rate, Ship, ReShip, ShipBatch, ReProcess, ModifyPackageList, VoidPackage, VoidBatch,
    Print, PrintShipmentReceipt, PrintBatch, Closeout, Transmit, ProcessBatch, Pack,
    PackPartial, AddressValidation, OpenGroup, ModifyGroup, CloseGroup, CreateGroup,
    GetGroups, GetGroup, UpdateNameAddress, Search, SearchHistory, FuelSurcharge,
    Track, ShipOrder, CancelPickup, CreatePickup, DBInsert, DBSelect, DBUpdate,
    DBDelete, LicenseValidation, Admin, Email, Clone, ConvertImage, ConfigDbProvider,
    AppDbProvider, UserMethod, Notifications, NotificationHub, CommodityContent,
    ImportCompanyConfiguration, HazmatContent, PendingShipment, Profile, Site,
    LicensePlate, SoxDbProvider, ImportBoxTypes, ExportBoxTypes, ImportHazmatConments,
    ExportHazmatContents, ImportCommodityContents, ExportCommodityContents,
    ImportUserConfigurations, ExportUserConfigurations, ImportAddressBookEntries,
    ExportAddressBookEntries, ImportValidationList, ExportValidationList, UpsPremier,
    BrCreateGroup, BrModifyGroup, Unauthorized, ServerError, AddUser, RemoveUser,
    UpdateUser, GetUser, UserRegistration, ValidateDate, SBRLoad, AdapterLoad,
    PackAdapter, PickupAdapter, ShipAdapter, TrackAdapter, PackRate, ShipDistribution,
    Unknown
}
```

### PSI.Sox.Tracking Namespace

```csharp
public enum TrackingStatus { Delivered, Exception, InTransit, Ready, Informational, Unknown }
```

### PSI.Sox.Packing Namespace

```csharp
public enum ContainerType { Carton, Pallet }
public enum LoadDirection { Any, Vertical, Horizontal }
public enum Orientation { Length, Width, Height, VlChannel, VlInterlock, Hollow, Insert }
```

### PSI.Sox.Data Namespace

```csharp
public enum ExportSettingType { Package, EventLog, CustomData }
public enum FieldType { Int, Long, Decimal, String, Bool, DateTime, EnumBatchStatus, EnumPortType, EnumParity, EnumStopBits, EnumHandshake, EnumPrintDirection, EnumPrintActions, Guid, EnumOperationType, EnumOperationDataType }
public enum SearchOperator { Equals, GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual, Contains, StartsWith, EndsWith, NotEqual }
```

### PSI.Sox.Adapter Namespace

```csharp
public enum DocumentDataType { PDoc, Zpl, Epl, Pdf }
```

### PSI.Sox.Tools Namespace

```csharp
public enum PrinterLanguage { Zpl, Epl, Pcl }
public enum Encoding { None, Base64 }
public enum LabelSize { Size4x6, Size4x65, Size4x8, SizeLetter, SizeLegal }
public enum RFIDDataType { Hex, Ascii, EPC }
```

### Types That Look Like Enums But Are NOT

> These are classes with `Id`, `Symbol`, `Name`, `Value` properties. Use them like reference/lookup objects, not enums.

- `DryIcePurpose` — class with { Id, Symbol, Name, Value }
- `DryIceRegulationSet` — class with { Id, Symbol, Name, Value }

---

## Interfaces (Internal — Do NOT add to using statements)

### IBusinessObject

The interface implemented by `SoxBusinessRules`. Defines all hook methods.

### IBusinessObjectApi

Available as `BusinessObjectApi` property. Provides access to ShipExec system operations.

```csharp
namespace PSI.Sox.Interfaces
{
    public interface IBusinessObjectApi
    {
        List<ShipmentResponse> Rate(object userContext, ShipmentRequest request, List<Service> services, int sortType, SerializableDictionary userParams);
        List<HistoryPackage> SearchPackageHistory(object userContext, Data.SearchCriteria criteria, out int totalRecords);
        ClientProfile GetClientProfile(object userContext);
        List<Batch> GetBatches(object userContext, Data.SearchCriteria criteria, out int totalRecords);
        void RemoveBatch(object userContext, string batchReference);
        // Additional API methods
    }
}
```

### ILogger

Available as `Logger` property. Used for writing to ShipExec logs.

```csharp
namespace PSI.Sox.Interfaces
{
    public interface ILogger
    {
        void Log(object source, LogLevel level, string message);
        void Log(string source, LogLevel level, string message);
    }
}
```

---

## BusinessRuleMetadata Attribute

Applied to the `SoxBusinessRules` class to identify the compiled DLL.

```csharp
namespace PSI.Sox
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BusinessRuleMetadataAttribute : Attribute
    {
        public string Author { get; set; }
        public string AuthorEmail { get; set; }
        public string CompanyId { get; set; }           // GUID string
        public string Description { get; set; }
        public string Name { get; set; }                // Short name for the SBR
        public string Version { get; set; }             // Version string
    }
}
```

---

## Critical Type Gotchas for AI Code Generation

| Property | Correct Type | Common Mistake |
|----------|-------------|----------------|
| `PackageDefaults.Service` | `Service` (object) | Assigning a raw string |
| `PackageDefaults.Shipdate` | `Date` (PSI.Sox.Date) | Using `DateTime` directly |
| `PackageDefaults.Terms` | `string` | Assuming it's an enum |
| `PackageDefaults.Shipper` | `string` | Assuming it's a Shipper object |
| `CommodityContent.Quantity` | `long` | Using `int` |
| `CommodityContent.UnitValue` | `Money` | Using `decimal` or `double` |
| `CommodityContent.UnitWeight` | `Weight` | Using `double` |
| `PackageRequest.DeclaredValueAmount` | `Money` | Using `decimal` |
| `PackageRequest.Weight` | `Weight` | Using `double` |
| `PackageDefaults` type | `Package` | Confusing with `PackageRequest` |
| `userParams` type | `SerializableDictionary` | Using `Dictionary<string,string>` |
| `BusinessRuleSettings` type | `List<BusinessRuleSetting>` | Using `Dictionary` |
| `PackageDefaults.PackageExtras` | `SerializableDictionary` | Using `List<PackageRequest>` or `Dictionary` |

---

## Namespace & Using Statement Rules

**Required usings for SBR code:**
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using PSI.Sox.Packing;  // Only if using Pack hooks
```

**The namespace for SBR classes is always:**
```csharp
namespace PSI.Sox
{
    // All SBR classes live here
}
```

**NEVER add these:**
- `using PSI.Sox.Interfaces;` — Does not exist as a user-importable namespace
- `using PSI.Sox;` — You're already IN this namespace

---

## How to Use This Prompt

When generating or repairing SBR code:

1. **Always check this document** before assigning values to PSI.Sox properties
2. **Use `Service` objects** — never assign a string directly to a Service property; create a `new Service { Symbol = "..." }`
3. **Use `Date` for ship dates** — `new Date(DateTime.Now)`
4. **Use `Weight` and `Money` objects** — never assign raw numeric values to weight/money properties
5. **`CommodityContent.Quantity` is `long`** — not int
6. **`PackageDefaults` is type `Package`** — it has all the shipment-level properties
7. **`Packages` contains `PackageRequest` objects** — package-level overrides
8. **`SerializableDictionary`** is the custom dict type for userParams — not a standard .NET dictionary
9. **`List<BusinessRuleSetting>`** is the config store — access via Key/Value properties
