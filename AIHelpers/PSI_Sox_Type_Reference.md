# PSI.Sox Type Reference

This document provides detailed type information for the key PSI.Sox classes used in ShipExec SBR (Server Business Rules) and CBR (Client Business Rules) development. Use this to avoid type assignment mistakes.

---

## Table of Contents
- [ShipmentRequest](#shipmentrequest)
- [Package](#package)
- [PackageRequest](#packagerequest)
- [SerializableDictionary](#serializabledictionary)
- [Profile / IProfile](#profile--iprofile)
- [Shipment (CBR Helper)](#shipment-cbr-helper)
- [Supporting Types](#supporting-types)
- [Additional Types (Detailed)](#additional-types-detailed)
  - [Pickup](#pickup)
  - [ShipmentResponse](#shipmentresponse)
  - [DocumentRequest](#documentrequest)
  - [DocumentResponse](#documentresponse)
  - [PrinterMapping](#printermapping)
  - [ManifestItem](#manifestitem)
  - [ModifyPackageListResult](#modifypackagelistresult)
  - [TransmitItem](#transmititem)
  - [PackingRateRequest](#packingraterequests)
  - [PackingRateResponse](#packingrateresponse)
  - [BoxType](#boxtype)
  - [NameAddress](#nameaddress)
  - [CommodityContent](#commoditycontent)
  - [HazmatContent](#hazmatcontent)
  - [Tools (Template Helper)](#tools-template-helper-class)
  - [IBusinessObjectApi](#ibusinessobjectapi-interface)
  - [Inheritance Hierarchy (Error Handling)](#inheritance-hierarchy-error-handling)

---

## ShipmentRequest

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.ShipmentRequest`  
**Serializable:** Yes

The top-level object representing a shipment. Passed into most SBR hook methods (Load, PreShip, PostShip, PreRate, etc.) and CBR functions (NewShipment, PreShip, etc.).

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `PackageDefaults` | `Package` | Default package settings applied to all packages. Contains origin address, consignee, service, billing, etc. |
| `Packages` | `List<PackageRequest>` | The list of individual packages in the shipment. |
| `PrintConfiguration` | `PrintConfiguration` | Printer/label configuration for the shipment. |
| `ManualShipment` | `bool` | Whether this is a manual shipment entry. |
| `AllowEdit` | `bool` | Whether the shipment can be edited. |
| `PendingShipmentId` | `int` | ID of a pending (saved but not shipped) shipment. |

### Key Relationships
- `ShipmentRequest.PackageDefaults` → `Package` (the "defaults" object, NOT a request — it's a full `Package`)
- `ShipmentRequest.Packages` → `List<PackageRequest>` (individual package overrides)
- `ShipmentRequest.PackageDefaults.OriginAddress` → `NameAddress` (shipper/origin)
- `ShipmentRequest.PackageDefaults.Consignee` → `NameAddress` (destination)
- `ShipmentRequest.PackageDefaults.Service` → `Service` (selected shipping service)
- `ShipmentRequest.PackageDefaults.OriginAddress.CustomData` → `List<CustomData>` (user reference fields)
- `ShipmentRequest.PackageDefaults.Consignee.CustomData` → `List<CustomData>` (to reference fields)

### Usage Examples (SBR - C#)
```csharp
public ShipmentRequest Load(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams)
{
    // Access origin custom data
    var customData = shipmentRequest.PackageDefaults.OriginAddress.CustomData;

    // Set service
    shipmentRequest.PackageDefaults.Service = new Service { Symbol = "CONNECTSHIP_UPS.UPS.GND", Name = "UPS Ground" };

    // Iterate packages
    foreach (var pkg in shipmentRequest.Packages)
    {
        pkg.ShipperReference = "REF123";
    }

    return shipmentRequest;
}
```

### Usage Examples (CBR - JavaScript)
```javascript
this.NewShipment = function(shipmentRequest) {
    // PackageDefaults is the default package settings
    shipmentRequest.PackageDefaults.Consignee = { Country: "US" };
    shipmentRequest.PackageDefaults.Service = { Symbol: "CONNECTSHIP_UPS.UPS.GND", Name: "UPS Ground" };
    shipmentRequest.PackageDefaults.Terms = "SHIPPER";

    // Access individual packages
    shipmentRequest.Packages[0].ShipperReference = "REF123";
    shipmentRequest.Packages[0].CommodityClass = "CLASS_85";
};
```

---

## Package

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.Package`  
**Serializable:** Yes

The `Package` class represents a fully-resolved package with all shipping details, charges, and tracking information. It is used as:
- `ShipmentRequest.PackageDefaults` (default values for all packages)
- Parameter in `PrePrint`, `PostPrint`, `ErrorLabel`, `PreVoid`, `PostVoid`, `PreModifyPackageList`, etc.
- Items in `PostCloseManifest` packages list

### Properties (PascalCase — complete list)

#### Address & Contact
| Property | Type |
|----------|------|
| `Consignee` | `NameAddress` |
| `CustomsBroker` | `NameAddress` |
| `DocumentationConsignee` | `NameAddress` |
| `Exporter` | `NameAddress` |
| `ForwardingAgent` | `NameAddress` |
| `GoodsOrigin` | `NameAddress` |
| `ImporterOfRecord` | `NameAddress` |
| `OriginAddress` | `NameAddress` |
| `ReturnAddress` | `NameAddress` |
| `ThirdPartyBillingAddress` | `NameAddress` |
| `TransportationChargesThirdPartyBillingAddress` | `NameAddress` |
| `DutiesAndTaxesThirdPartyBillingAddress` | `NameAddress` |
| `UltimateConsignee` | `NameAddress` |
| `BrokerageThirdPartyBillingAddress` | `NameAddress` |
| `CodReturnAddress` | `NameAddress` |
| `ConsigneeThirdPartyBillingAddress` | `NameAddress` |
| `HoldAtLocationAddress` | `NameAddress` |

#### References & Identifiers
| Property | Type |
|----------|------|
| `ShipperReference` | `string` |
| `ConsigneeReference` | `string` |
| `MiscReference1` through `MiscReference20` | `string` |
| `TrackingNumber` | `string` |
| `TrackingNumber2` | `string` |
| `BarCode` | `string` |
| `BarCode2` | `string` |
| `BarCode3` | `string` |
| `BatchReference` | `string` |
| `BatchItemReference` | `string` |
| `ShipmentId` | `string` |
| `WaybillBolNumber` | `string` |
| `SerialNumber` | `string` |
| `SubNumber` | `string` |
| `TransactionId` | `string` |

#### Service & Carrier
| Property | Type |
|----------|------|
| `Service` | `Service` |
| `ShippedService` | `Service` |
| `RatedService` | `Service` |
| `OriginalService` | `Service` |
| `Carrier` | `Carrier` |
| `CarrierName` | `string` |

#### Physical Dimensions & Weight
| Property | Type |
|----------|------|
| `Weight` | `Weight` |
| `RatedWeight` | `Weight` |
| `PackagingTareWeight` | `Weight` |
| `DryIceWeight` | `Weight` |
| `Dimensions` | `Dimensions` |
| `BoxType` | `BoxType` |
| `PieceCount` | `int64` |
| `HandlingUnitCount` | `int64` |

#### Charges (all `Money` type)
| Property | Type |
|----------|------|
| `Total` | `Money` |
| `BaseCharge` | `Money` |
| `Discount` | `Money` |
| `Special` | `Money` |
| `Tax` | `Money` |
| `FuelSurcharge` | `Money` |
| `ApportionedTotal` | `Money` |
| `ApportionedBase` | `Money` |
| `ApportionedDiscount` | `Money` |
| `ApportionedSpecial` | `Money` |
| `DeclaredValueAmount` | `Money` |
| `DeclaredValueCustoms` | `Money` |
| `DeclaredValueFee` | `Money` |
| `CodAmount` | `Money` |
| `CodFee` | `Money` |
| `OriginalRate` | `Money` |
| *(many more service-specific fees)* | `Money` |

#### Shipping Options (bool)
| Property | Type |
|----------|------|
| `AdditionalHandling` | `bool` |
| `SaturdayDelivery` | `bool` |
| `SundayDelivery` | `bool` |
| `HoldAtLocation` | `bool` |
| `SignatureRelease` | `bool` |
| `ProofRequireSignature` | `bool` |
| `ProofRequireSignatureAdult` | `bool` |
| `ProofRequireSignatureConsignee` | `bool` |
| `ReturnDelivery` | `bool` |
| `ThirdPartyBilling` | `bool` |
| `ConsigneeThirdPartyBilling` | `bool` |
| `InsideDelivery` | `bool` |
| `LiftgateDelivery` | `bool` |
| `LiftgatePickup` | `bool` |
| `CarbonNeutral` | `bool` |
| `DocumentsOnly` | `bool` |
| `Exchange` | `bool` |
| `Calltag` | `bool` |

#### Notification
| Property | Type |
|----------|------|
| `ShipNotificationEmail` | `bool` |
| `ShipNotificationAddressEmail` | `string` |
| `ShipNotificationFax` | `bool` |
| `ShipNotificationAddressFax` | `string` |
| `ShipNotificationVerbal` | `bool` |
| `ShipNotificationDescription` | `string` |
| `ShipNotificationSenderName` | `string` |
| `ShipNotificationSubjectText` | `string` |
| `DeliveryNotificationEmail` | `bool` |
| `DeliveryNotificationAddressEmail` | `string` |
| `DeliveryExceptionNotification` | `bool` |
| `DeliveryExceptionNotificationAddressEmail` | `string` |

#### Dates
| Property | Type |
|----------|------|
| `Shipdate` | `Date` |
| `ArriveDate` | `Date` |
| `DeliverDate` | `Date` |
| `EarliestDeliveryDate` | `Date` |
| `LatestDeliveryDate` | `Date` |
| `OriginatorShipdate` | `Date` |

#### International / Customs
| Property | Type |
|----------|------|
| `CommodityClass` | `string` |
| `CommodityCondition` | `int` |
| `CommodityContents` | `List<CommodityContent>` |
| `CommercialInvoiceMethod` | `int` |
| `ExportReason` | `string` |
| `ExportDeclarationStatement` | `string` |
| `ExportInformationCode` | `string` |
| `TermsOfSale` | `string` |
| `DutiesAndTaxes` | `string` |
| `SplitDutiesAndTaxes` | `bool` |
| `AesTransactionNumber` | `string` |
| `UltimateDestinationCountry` | `string` |
| `InbondCode` | `int` |
| `HazmatContents` | `List<HazmatContent>` |
| `AlcoholContents` | `List<AlcoholContent>` |

#### Extras & Custom Data
| Property | Type | Description |
|----------|------|-------------|
| `PackageExtras` | `SerializableDictionary` | Key/value pairs for custom package data. This is a `Dictionary<string, object>`. |
| `ParentContainer` | `SerializableDictionary` | Parent container information as key/value pairs. |
| `UserData1` through `UserData5` | `object` | General-purpose user data slots. |
| `Description` | `string` | Package description text. |
| `Comments` | `string` | Package comments. |
| `PackagingDescription` | `string` | Packaging type description. |

#### Billing
| Property | Type |
|----------|------|
| `ThirdPartyBilling` | `bool` |
| `ThirdPartyBillingAddress` | `NameAddress` |
| `ConsigneeThirdPartyBilling` | `bool` |
| `ConsigneeThirdPartyBillingAddress` | `NameAddress` |
| `BrokerageThirdPartyBilling` | `bool` |
| `BrokerageThirdPartyBillingAddress` | `NameAddress` |
| `TransportationCharges` | `string` |

#### IDs & System
| Property | Type |
|----------|------|
| `GlobalMsn` | `int64` |
| `ShipId` | `int64` |
| `ManifestId` | `int64` |
| `ManifestName` | `string` |
| `ManifestSymbol` | `string` |
| `CompanyId` | `Guid` |
| `SiteId` | `Guid?` |
| `UserId` | `Guid` |
| `BatchSequenceNumber` | `int64` |
| `CycleCount` | `int64` |
| `LocationId` | `string` |
| `MachineName` | `string` |

#### Misc
| Property | Type |
|----------|------|
| `ReturnAddressMethod` | `int` |
| `ReturnDeliveryMethod` | `int` |
| `DeliveryMethod` | `int` |
| `CodMethod` | `int` |
| `CodPaymentMethod` | `int` |
| `InsuranceMethod` | `int` |
| `BrokerageMethod` | `int` |
| `TransportMode` | `int` |
| `CarrierTenderMethod` | `int` |
| `OverRideShipmentCriteria` | `bool` |
| `Zone` | `string` |
| `TimeInTransit` | `string` |
| `TimeInTransitDays` | `int` |
| `DimensionalWeightRated` | `bool` |
| `DistributionCode` | `string` |

---

## PackageRequest

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.PackageRequest`  
**Serializable:** Yes

Represents an individual package within a `ShipmentRequest.Packages` list. Contains a subset of `Package` properties — only those that can be set per-package as overrides to `PackageDefaults`.

### Key Differences from Package
- **PackageRequest** does NOT have fee/charge properties (no `Total`, `BaseCharge`, `FuelSurcharge`, etc.)
- **PackageRequest** does NOT have response-only properties (`ShippedService`, `RatedService`, `RatedWeight`, `GlobalMsn`, `ManifestId`, etc.)
- **PackageRequest** DOES have all the settable shipping properties

### Properties (complete list)

| Property | Type |
|----------|------|
| `AdditionalHandling` | `bool` |
| `AdditionalHandlingType` | `int` |
| `AdditionalHardcopyDocumentation` | `bool` |
| `AddressChangeNotification` | `bool` |
| `AdultMinimumAge` | `short` |
| `AesTransactionNumber` | `string` |
| `AlcoholContents` | `List<AlcoholContent>` |
| `AppointmentDelivery` | `bool` |
| `BarCode` | `string` |
| `BarCode2` | `string` |
| `BarCode3` | `string` |
| `BatchItemReference` | `string` |
| `BatchReference` | `string` |
| `BatchSequenceNumber` | `int64` |
| `BolComment` | `string` |
| `BolLegalStatement` | `string` |
| `BoxType` | `BoxType` |
| `BrokerageMethod` | `int` |
| `BrokerageThirdPartyBilling` | `bool` |
| `BrokerageThirdPartyBillingAddress` | `NameAddress` |
| `CalltagNumber` | `string` |
| `CarbonNeutral` | `bool` |
| `CarrierInstructions` | `string` |
| `CarrierMonitoring` | `bool` |
| `CarrierMonitoringPurpose` | `int` |
| `CarrierName` | `string` |
| `CarrierTenderMethod` | `int` |
| `CertifiedMail` | `bool` |
| `CertOfOriginMethod` | `int` |
| `ChainOfSignature` | `bool` |
| `ChargesOnDocumentation` | `int` |
| `CodAlternateNumber` | `bool` |
| `CodAmount` | `Money` |
| `CodInstructions` | `string` |
| `CodMasterTrackingNumber` | `string` |
| `CodMethod` | `int` |
| `CodNumber` | `string` |
| `CodPaymentMethod` | `int` |
| `CodPaymentMethodPostDatedCheckDate` | `Date` |
| `CodPayorAddressEmail` | `string` |
| `CodPayorInstructions` | `string` |
| `CodPendingFeePayorPercentage` | `decimal` |
| `CodReturnAddress` | `NameAddress` |
| `CodReturnMethod` | `int` |
| `CodReturnTrackingNumber` | `string` |
| `Comments` | `string` |
| `CommercialInvoiceMethod` | `int` |
| `CommitmentCode` | `string` |
| `CommodityClass` | `string` |
| `CommodityCondition` | `int` |
| `CommodityContents` | `List<CommodityContent>` |
| `CompanyId` | `Guid` |
| `Consignee` | `NameAddress` |
| `ConsigneeBillingId` | `string` |
| `ConsigneeCustomsId` | `string` |
| `ConsigneeReference` | `string` |
| `ConsigneeThirdPartyBilling` | `bool` |
| `ConsigneeThirdPartyBillingAddress` | `NameAddress` |
| `ConsolidationCarrier` | `string` |
| `ConsolidationCode` | `string` |
| `ConsolidationFlag` | `bool` |
| `ConsolidationId` | `int64` |
| `ConsolidationShipmentId` | `string` |
| `ConsolidationTrackingNumber` | `string` |
| `ConsolidationType` | `string` |
| `ContainerCode` | `string` |
| `CustomsBroker` | `NameAddress` |
| `CycleCount` | `int64` |
| `DeclaredValueAmount` | `Money` |
| `DeclaredValueCustoms` | `Money` |
| `DeconsolidationCarrier` | `string` |
| `DeliverDate` | `Date` |
| `DeliverToDoor` | `bool` |
| `DeliveryAreaCode` | `string` |
| `DeliveryExceptionNotification` | `bool` |
| `DeliveryExceptionNotificationAddressEmail` | `string` |
| `DeliveryExceptionNotificationDescription` | `string` |
| `DeliveryExceptionNotificationEmail` | `bool` |
| `DeliveryExceptionNotificationSenderName` | `string` |
| `DeliveryExceptionNotificationSubjectText` | `string` |
| `DeliveryMethod` | `int` |
| `DeliveryNotificationAddressEmail` | `string` |
| `DeliveryNotificationDescription` | `string` |
| `DeliveryNotificationEmail` | `bool` |
| `DeliveryNotificationSenderName` | `string` |
| `DeliveryNotificationSubjectText` | `string` |
| `Description` | `string` |
| `Dimensions` | `Dimensions` |
| `DispositionMethod` | `int` |
| `DistributionCode` | `string` |
| `DocumentsOnly` | `bool` |
| `DryIcePurpose` | `int` |
| `DryIceRegulationSet` | `int` |
| `DryIceWeight` | `Weight` |
| `DutiesAndTaxes` | `string` |
| `DutiesAndTaxesThirdPartyBillingAddress` | `NameAddress` |
| `ExportDeclarationStatement` | `string` |
| `Exporter` | `NameAddress` |
| `ExportInformationCode` | `string` |
| `ExportReason` | `string` |
| `ForwardingAgent` | `NameAddress` |
| `GoodsInFreeCirculation` | `bool` |
| `GoodsOrigin` | `NameAddress` |
| `HazmatContents` | `List<HazmatContent>` |
| `HazmatHandlingInformation` | `string` |
| `HoldAtLocation` | `bool` |
| `HoldAtLocationAddress` | `NameAddress` |
| `HoldAtLocationFacilityId` | `string` |
| `HoldAtLocationType` | `int` |
| `ImporterOfRecord` | `NameAddress` |
| `InbondCode` | `int` |
| `InsuranceMethod` | `int` |
| `MiscReference1` through `MiscReference20` | `string` |
| `OriginalRate` | `Money` |
| `OriginalService` | `Service` |
| `OriginatorShipdate` | `Date` |
| `OriginatorTrackingNumber` | `string` |
| `OriginDescription` | `string` |
| `OverRideShipmentCriteria` | `bool` |
| `Oversize` | `bool` |
| `PackageExtras` | `SerializableDictionary` |
| `PackagingDescription` | `string` |
| `PackagingTareWeight` | `Weight` |
| `PalletJackDelivery` | `bool` |
| `PalletJackPickup` | `bool` |
| `ParcelAirlift` | `bool` |
| `ParentContainer` | `SerializableDictionary` |
| `ParentContainerCode` | `string` |
| `PartiesRelated` | `bool` |
| `Perishable` | `bool` |
| `PharmacyDelivery` | `bool` |
| `PickupTime` | `string` |
| `PieceCount` | `int64` |
| `ProofNumber` | `string` |
| `ProofRequireSignature` | `bool` |
| `ProofRequireSignatureAdult` | `bool` |
| `ProofRequireSignatureConsignee` | `bool` |
| `ProofReturnOfDocuments` | `bool` |
| `ProofSignatureWaiver` | `bool` |
| `ProofUseAlternateNumber` | `bool` |
| `RateCode` | `string` |
| `ReasonForUpgrade` | `string` |
| `RegisteredMail` | `bool` |
| `RemotePassthrough` | `string` |
| `ReturnAddress` | `NameAddress` |
| `ReturnAddressMethod` | `int` |
| `ReturnDelivery` | `bool` |
| `ReturnDeliveryAddressEmail` | `string` |
| `ReturnDeliveryAddressEmailLocale` | `string` |
| `ReturnDeliveryMethod` | `int` |
| `ReturnDeliveryNotificationAddress` | `NameAddress` |
| `ReturnDeliveryNotificationAddress2` | `NameAddress` |
| `ReturnDeliveryNotificationAddressEmail` | `string` |
| `ReturnDeliveryNotificationDescription` | `string` |
| `ReturnDeliveryNotificationEmail` | `bool` |
| `ReturnDeliveryNotificationFax` | `bool` |
| `ReturnDeliveryNotificationSenderName` | `string` |
| `ReturnDeliveryNotificationSubjectText` | `string` |
| `ReturnTrackingRetentionDays` | `int64` |
| `RoutedExportTransaction` | `bool` |
| `RoutingCode` | `string` |
| `RoutingCode2` through `RoutingCode5` | `string` |
| `SaturdayDelivery` | `bool` |
| `Security` | `bool` |
| `SedExemptionNumber` | `string` |
| `SedMethod` | `int` |
| `SerialNumber` | `string` |
| `Service` | `Service` |
| `Shipdate` | `Date` |
| `ShipId` | `int64` |
| `ShipNotificationAddress` | `NameAddress` |
| `ShipNotificationAddress2` | `NameAddress` |
| `ShipNotificationAddressEmail` | `string` |
| `ShipNotificationAddressFax` | `string` |
| `ShipNotificationDescription` | `string` |
| `ShipNotificationEmail` | `bool` |
| `ShipNotificationFax` | `bool` |
| `ShipNotificationSenderName` | `string` |
| `ShipNotificationSubjectText` | `string` |
| `ShipNotificationVerbal` | `bool` |
| `ShipperReference` | `string` |
| `SignatureRelease` | `bool` |
| `SiteId` | `Guid?` |
| `SpecialDelivery` | `bool` |
| `SplitDutiesAndTaxes` | `bool` |
| `StairDelivery` | `bool` |
| `StairPickup` | `bool` |
| `SubNumber` | `string` |
| `SundayDelivery` | `bool` |
| `SuppressDc` | `bool` |
| `SuppressMms` | `bool` |
| `Tax` | `Money` |
| `TemperatureControl` | `int` |
| `TermsOfSale` | `string` |
| `ThirdPartyBilling` | `bool` |
| `ThirdPartyBillingAddress` | `NameAddress` |
| `TrackingNumber` | `string` |
| `TrackingNumber2` | `string` |
| `TransportationCharges` | `string` |
| `TransportationChargesThirdPartyBillingAddress` | `NameAddress` |
| `TransportMode` | `int` |
| `UltimateConsignee` | `NameAddress` |
| `UltimateConsigneeType` | `int` |
| `UltimateDestinationCountry` | `string` |
| `Unpack` | `bool` |
| `UserData1` through `UserData5` | `object` |
| `UserId` | `Guid` |
| `WaybillBolNumber` | `string` |
| `Weight` | `Weight` |
| `WorldEaseCode` | `string` |
| `WorldEaseFlag` | `bool` |
| `WorldEaseId` | `int64` |
| `WorldEaseMasterShipmentId` | `string` |
| `WorldEaseSingleEuCountry` | `bool` |

---

## SerializableDictionary

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.SerializableDictionary`  
**Base Class:** `Dictionary<string, object>`  
**Implements:** `ICloneable`  
**Serializable:** Yes

A dictionary of string keys to object values used for:
- `userParams` parameter in all SBR hook methods
- `Package.PackageExtras` — custom per-package key/value data
- `Package.ParentContainer` — parent container information

### Usage (SBR - C#)
```csharp
// Reading from userParams
if (userParams.ContainsKey("MyKey"))
{
    string val = userParams["MyKey"].ToString();
}

// Writing to userParams
userParams["OutputKey"] = "some value";

// Reading PackageExtras
var extras = shipmentRequest.PackageDefaults.PackageExtras;
if (extras != null && extras.ContainsKey("CustomField"))
{
    var value = extras["CustomField"];
}
```

### Usage (CBR - JavaScript)
```javascript
// PackageExtras in CBR
var extras = shipmentRequest.PackageDefaults.PackageExtras;
if (extras && extras["CustomField"]) {
    var val = extras["CustomField"];
}
```

### Important Notes
- Keys are `string`, values are `object` — always cast/convert when reading
- Implements standard `Dictionary<string, object>` methods: `ContainsKey()`, indexer `[]`, `Add()`, `Remove()`, `Keys`, `Values`, `Count`
- Serialized using WCF DataContract serialization with item name "Item", key name "Key", value name "Value"

---

## Profile / IProfile

**Namespace:** `PSI.Sox` (Profile class) / `PSI.Sox.Interfaces` (IProfile interface)  
**Full Name:** `PSI.Sox.Profile`  
**Serializable:** Yes

Available in SBR as `this.Profile` (type `IProfile`). Contains all configuration data for the current user's profile including available services, shippers, carriers, and settings.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Profile ID |
| `Name` | `string` | Profile name |
| `Description` | `string` | Profile description |
| `CompanyId` | `Guid` | Company GUID |
| `CompanyName` | `string` | Company name |
| `SiteId` | `Guid?` | Site GUID (nullable) |
| `SiteName` | `string` | Site name |
| `Services` | `List<Service>` | Available shipping services |
| `Shippers` | `List<Shipper>` | Available shippers/origin locations |
| `Carriers` | `List<Carrier>` | Available carriers |
| `BoxTypes` | `List<BoxType>` | Available box types |
| `PackageTypes` | `List<ProfilePackageType>` | Package type options |
| `PaymentTerms` | `List<ProfilePaymentTerm>` | Payment term options |
| `PaymentTypes` | `List<PaymentType>` | Payment type options |
| `CommodityClasses` | `List<CommodityClass>` | Commodity class options |
| `CommodityConditions` | `List<CommodityCondition>` | Commodity condition options |
| `CommodityContents` | `List<CommodityContent>` | Commodity content definitions |
| `Countries` | `List<Country>` | Country options |
| `Currencies` | `List<Currency>` | Currency options |
| `TransportModes` | `List<TransportMode>` | Transport mode options |
| `ReturnAddressMethods` | `List<ReturnAddressMethod>` | Return address method options |
| `ReturnDeliveryMethods` | `List<ReturnDeliveryMethod>` | Return delivery method options |
| `DeliveryMethods` | `List<DeliveryMethod>` | Delivery method options |
| `CodMethods` | `List<CodMethod>` | COD method options |
| `CodPaymentMethods` | `List<CodPaymentMethod>` | COD payment options |
| `CodReturnMethods` | `List<CodReturnMethod>` | COD return method options |
| `CommercialInvoiceMethods` | `List<CommercialInvoiceMethod>` | Invoice method options |
| `InsuranceMethods` | `List<InsuranceMethod>` | Insurance method options |
| `BrokerageMethods` | `List<BrokerageMethod>` | Brokerage method options |
| `SedMethods` | `List<SedMethod>` | SED method options |
| `ExchangeMethods` | `List<ExchangeMethod>` | Exchange method options |
| `DispositionMethods` | `List<DispositionMethod>` | Disposition method options |
| `GroupTypes` | `List<GroupType>` | Group type options |
| `IncoTerms` | `List<IncoTerm>` | Incoterms options |
| `InbondCodes` | `List<InbondCode>` | Inbond code options |
| `HazMatRegulations` | `List<HazmatRegulation>` | Hazmat regulation options |
| `HazMatRegulationSets` | `List<HazmatRegulationSet>` | Hazmat regulation set options |
| `FieldOptions` | `List<FieldOption>` | Field visibility/requirement options |
| `Templates` | `List<Template>` | Template options |
| `Reports` | `List<Report>` | Report options |
| `UnitOfMeasures` | `List<UnitOfMeasure>` | Unit of measure options |
| `UserInformation` | `UserInformation` | Current user's information |
| `ProfileSettings` | `ProfileSettings` or `ClientProfileSettings` | Profile-level settings |
| `PrinterConfiguration` | `PrinterConfiguration` | Printer settings |
| `ScaleConfiguration` | `ScaleConfiguration` | Scale settings |
| `DocumentConfiguration` | `DocumentConfiguration` | Document settings |
| `ServerBusinessRuleId` | `int` | Associated SBR ID |
| `ServerBusinessRuleOptions` | `ServerBusinessRuleOptions` | SBR options |
| `ClientBusinessRuleId` | `int` | Associated CBR ID |
| `ClientBusinessRuleOptions` | `ClientBusinessRuleOptions` | CBR options |
| `UseHardwareSupport` | `bool` | Whether hardware support is enabled |
| `HardwareSupportUrl` | `string` | Hardware support service URL |
| `ProfilePickupConfigurations` | `List<ProfilePickupConfiguration>` | Pickup configurations |
| `AlcoholPackagings` | `List<AlcoholPackaging>` | Alcohol packaging types |
| `AlcoholTypes` | `List<AlcoholType>` | Alcohol type options |
| `DryIcePurposes` | `List<DryIcePurpose>` | Dry ice purpose options |
| `DryIceRegulationSets` | `List<DryIceRegulationSet>` | Dry ice regulation sets |
| `PickupTypes` | `List<PickupType>` | Pickup type options |
| `AdditionalHandlingTypes` | `List<AdditionalHandlingType>` | Additional handling type options |
| `RestrictedArticleTypes` | `List<RestrictedArticleType>` | Restricted article types |

### UserInformation Sub-Type
| Property | Type | Description |
|----------|------|-------------|
| `Address` | `NameAddress` | User's address (includes `CustomData` for user reference fields) |

### Usage (SBR - C#)
```csharp
// Access user's shippers
List<Shipper> shippers = Profile.Shippers;

// Access user custom data (reference fields)
var userAddress = Profile.UserInformation.Address;
var customData = userAddress.CustomData; // List<CustomData>

// Access available services
var services = Profile.Services;
foreach (var svc in services)
{
    string name = svc.Name;
    string symbol = svc.Symbol;
}
```

### Usage (CBR - JavaScript)
```javascript
// In CBR, profile is accessed via vm.profile
var services = this.vm.profile.Services;
var shippers = this.vm.profile.Shippers;
var userInfo = this.vm.profile.UserInformation;
var userCustomData = userInfo.Address.CustomData;
```

---

## Shipment (CBR Helper)

> **NOTE:** There is NO `PSI.Sox.Shipment` class in the DLL. The "Shipment" class in CBR/SBR code is a **custom helper wrapper** around `ShipmentRequest` that provides convenient access to user reference fields stored in `CustomData`.

### Pattern (JavaScript CBR)
```javascript
function GetShipment(shipmentRequest) {
    class Shipment {
        constructor(shipmentRequest) {
            this.ShipmentRequest = shipmentRequest;
        }

        get UserRef1() { return this.GetCustom("Custom1", "User"); }
        get UserRef2() { return this.GetCustom("Custom2", "User"); }
        // ... UserRef3 through UserRef10

        get ToRef1() { return this.GetCustom("Custom1", "To"); }
        get ToRef2() { return this.GetCustom("Custom2", "To"); }
        // ... ToRef3 through ToRef10

        GetCustom(fieldName, customDataType) {
            var returnValue = "";
            var customData = null;
            if (customDataType === "User") 
                customData = this.ShipmentRequest.PackageDefaults.OriginAddress.CustomData;
            if (customDataType === "To") 
                customData = this.ShipmentRequest.PackageDefaults.Consignee.CustomData;

            if (customData) {
                customData.forEach(function(customField) {
                    if (fieldName === customField.Key) returnValue = customField.Value;
                });
            }
            return returnValue;
        }
    }
    return new Shipment(shipmentRequest);
}
```

### Properties
| Property | Type | Source |
|----------|------|--------|
| `ShipmentRequest` | `ShipmentRequest` | The wrapped request object |
| `UserRef1` through `UserRef10` | `string` (getter) | `PackageDefaults.OriginAddress.CustomData["Custom1"]` through `["Custom10"]` |
| `ToRef1` through `ToRef10` | `string` (getter) | `PackageDefaults.Consignee.CustomData["Custom1"]` through `["Custom10"]` |

### Usage
```javascript
var shipment = GetShipment(shipmentRequest);
var department = shipment.UserRef1;  // reads Custom1 from OriginAddress.CustomData
var destRef = shipment.ToRef3;       // reads Custom3 from Consignee.CustomData

// Then use in package assignment:
shipmentRequest.Packages[0].ShipperReference = shipment.UserRef1;
shipmentRequest.Packages[0].ConsigneeReference = shipment.UserRef4;
```

---

## Supporting Types

### NameAddress
Used for all address fields (Consignee, OriginAddress, ThirdPartyBillingAddress, etc.)

| Property | Type | Description |
|----------|------|-------------|
| `Company` | `string` | Company name |
| `Contact` | `string` | Contact name |
| `Address1` | `string` | Address line 1 |
| `Address2` | `string` | Address line 2 |
| `Address3` | `string` | Address line 3 |
| `City` | `string` | City |
| `StateProvince` | `string` | State or province |
| `PostalCode` | `string` | Postal/ZIP code |
| `Country` | `string` | Country code (e.g., "US", "CA") |
| `Phone` | `string` | Phone number |
| `Fax` | `string` | Fax number |
| `Email` | `string` | Email address |
| `Sms` | `string` | SMS number |
| `Account` | `string` | Account number |
| `Code` | `string` | Address code |
| `Group` | `string` | Address group |
| `TaxId` | `string` | Tax identification number |
| `TaxIdType` | `TaxIdType` (enum) | Tax ID type |
| `Residential` | `bool` | Whether residential address |
| `PoBox` | `bool` | Whether PO Box |
| `IsValidated` | `bool` | Whether address has been validated |
| `CustomData` | `List<CustomData>` | Custom key/value fields (Custom1-Custom10 = user reference fields) |

### Service
| Property | Type | Description |
|----------|------|-------------|
| `Symbol` | `string` | Service symbol (e.g., "CONNECTSHIP_UPS.UPS.GND") |
| `Name` | `string` | Display name (e.g., "UPS Ground") |
| `Code` | `int` | Service code |
| `Id` | `int` | Service ID |
| `Carrier` | `string` | Carrier portion of symbol |
| `CarrierId` | `int` | Carrier ID |

### Money
| Property | Type | Description |
|----------|------|-------------|
| `Amount` | `double` | Monetary amount |
| `Currency` | `string` | Currency code (e.g., "USD") |

### Weight
| Property | Type | Description |
|----------|------|-------------|
| `Amount` | `double` | Weight value |
| `Units` | `string` | Weight units (e.g., "LB", "KG") |

### Dimensions
| Property | Type | Description |
|----------|------|-------------|
| `Length` | `double` | Length |
| `Width` | `double` | Width |
| `Height` | `double` | Height |
| `Units` | `DimensionUnits` (enum) | Dimension units |

### Date
| Property | Type | Description |
|----------|------|-------------|
| `Year` | `int` | Year |
| `Month` | `int` | Month |
| `Day` | `int` | Day |
| `DayOfWeek` | `DayOfWeek` | Day of week |
| `DayOfYear` | `int` | Day of year |
| `DateAsString` | `string` | Date as formatted string |

### CustomData
| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Field name (e.g., "Custom1" through "Custom10") |
| `Value` | `string` | Field value |

### Shipper
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Shipper ID |
| `Name` | `string` | Shipper display name |
| `Symbol` | `string` | Shipper symbol |
| `Code` | `string` | Shipper code |
| `Company` | `string` | Company name |
| `Contact` | `string` | Contact name |
| `Address1` | `string` | Address line 1 |
| `Address2` | `string` | Address line 2 |
| `Address3` | `string` | Address line 3 |
| `City` | `string` | City |
| `StateProvince` | `string` | State/Province |
| `PostalCode` | `string` | Postal code |
| `Country` | `string` | Country code |
| `Phone` | `string` | Phone |
| `Fax` | `string` | Fax |
| `Email` | `string` | Email |
| `Sms` | `string` | SMS |
| `CompanyId` | `Guid` | Company GUID |
| `SiteId` | `Guid?` | Site GUID |
| `Residential` | `bool` | Residential flag |
| `PoBox` | `bool` | PO Box flag |
| `Address` | `NameAddress` | Full address object |
| `Carriers` | `List<Carrier>` | Associated carriers |
| `CustomData` | `List<CustomData>` | Custom data fields |
| `CustomDataXml` | `string` | Custom data as XML |

### Carrier
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Carrier ID |
| `Name` | `string` | Carrier name |
| `Symbol` | `string` | Carrier symbol (e.g., "CONNECTSHIP_UPS.UPS") |
| `CompanyId` | `Guid` | Company GUID |
| `AdapterRegistrationId` | `int` | Adapter registration ID |

### BoxType
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Box type ID |
| `Name` | `string` | Box type name |
| `Dimensions` | `Dimensions` | Box dimensions |
| `Sequence` | `int` | Display sequence |
| `CompanyId` | `Guid` | Company GUID |
| `SiteId` | `Guid?` | Site GUID |

### BusinessRuleSetting
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Setting ID |
| `Key` | `string` | Setting key name |
| `Value` | `string` | Setting value |
| `ServerBusinessRuleId` | `int` | Associated SBR ID |

### ClientContext
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Context GUID |
| `Company` | `CompanyInfo` | Company information |
| `Site` | `SiteInfo` | Site information |
| `User` | `UserInfo` | User information |
| `Machine` | `MachineInfo` | Machine information |

#### CompanyInfo
| Property | Type |
|----------|------|
| `Id` | `Guid` |
| `Name` | `string` |
| `Symbol` | `string` |

#### SiteInfo
| Property | Type |
|----------|------|
| `Id` | `Guid` |
| `Name` | `string` |

#### UserInfo
| Property | Type |
|----------|------|
| `Id` | `Guid` |
| `UserName` | `string` |
| `Email` | `string` |

#### MachineInfo
| Property | Type |
|----------|------|
| `Name` | `string` |

---

## Additional Types (Detailed)

### Pickup

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.Pickup`

Used in the `Ship` SBR hook method as a parameter for scheduling pickups.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Pickup ID |
| `Carrier` | `string` | Carrier symbol |
| `Service` | `Service` | Service for pickup |
| `PickupType` | `PickupType` | Type of pickup |
| `PickupAddress` | `NameAddress` | Pickup location address |
| `PickupDate` | `Date` | Scheduled pickup date |
| `PickupReferenceNumber` | `string` | Reference number for pickup |
| `PickupStatus` | `PickupStatus` (enum) | Status of the pickup |
| `PaymentType` | `PaymentType` | Payment type for pickup |
| `Weight` | `Weight` | Total weight |
| `Quantity` | `int` | Number of packages |
| `PackagingType` | `string` | Packaging type |
| `EarliestTimeReady` | `string` | Earliest time ready for pickup |
| `LatestTimeReady` | `string` | Latest time ready for pickup |
| `ContactName` | `string` | Contact name |
| `ContactPhone` | `string` | Contact phone |
| `Comment` | `string` | Comments |
| `Description` | `string` | Description |
| `DestinationCountryCode` | `string` | Destination country |
| `Floor` | `string` | Floor location |
| `Room` | `string` | Room location |
| `GlobalBundleId` | `int64` | Bundle ID |

---

### ShipmentResponse

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.ShipmentResponse`

Returned from `Ship`, `Rate`, and other operations. Contains the results including tracking numbers, charges, and errors.

| Property | Type | Description |
|----------|------|-------------|
| `PackageDefaults` | `Package` | Response package defaults (includes `ErrorCode`, `ErrorMessage` inherited from `CompoundErrorBase`/`ErrorBase`) |
| `Packages` | `List<PackageResponse>` | List of individual package responses |

#### Important: Error Handling
`ShipmentResponse.PackageDefaults` is a `Package` which extends `CompoundErrorBase`:
- `ErrorCode` : `int` (0 = success, non-zero = error) — inherited from base
- `ErrorMessage` : `string` — inherited from base
- `Errors` : `List<CompoundError>` — from `CompoundErrorBase`

`ShipmentResponse.Packages[i]` is a `PackageResponse` which extends `ErrorBase`:
- `ErrorCode` : `int` (0 = success)
- `ErrorMessage` : `string`

#### PackageResponse Key Properties
`PackageResponse` extends `ErrorBase` and contains response-only data:

| Property | Type | Description |
|----------|------|-------------|
| `ErrorCode` | `int` | 0 = success, non-zero = error (inherited) |
| `ErrorMessage` | `string` | Error description (inherited) |
| `TrackingNumber` | `string` | Assigned tracking number |
| `TrackingNumber2` | `string` | Secondary tracking number |
| `Service` | `Service` | Requested service |
| `ShippedService` | `Service` | Actual shipped service |
| `RatedService` | `Service` | Rated service |
| `Total` | `Money` | Total charge |
| `BaseCharge` | `Money` | Base charge |
| `Discount` | `Money` | Discount amount |
| `Special` | `Money` | Special charge |
| `Tax` | `Money` | Tax amount |
| `FuelSurcharge` | `Money` | Fuel surcharge |
| `ApportionedTotal` | `Money` | Apportioned total |
| `Weight` | `Weight` | Package weight |
| `RatedWeight` | `Weight` | Rated weight (may differ from actual) |
| `Documents` | `List<DocumentResponse>` | Printed document results (labels, etc.) |
| `ShipId` | `int64` | Ship transaction ID |
| `ShipmentId` | `string` | Shipment ID |
| `BarCode` | `string` | Barcode |
| `Zone` | `string` | Shipping zone |
| `TimeInTransit` | `string` | Transit time |
| `TimeInTransitDays` | `int` | Transit days |
| `ArriveDate` | `Date` | Expected arrival date |
| `PackageExtras` | `SerializableDictionary` | Custom extras |
| `UserData1` through `UserData5` | `object` | Custom user data |
| `BatchReference` | `string` | Batch reference |
| `BatchItemReference` | `string` | Batch item reference |
| *(all fee properties)* | `Money` | Various carrier fees |

#### Usage (SBR - C#)
```csharp
public void PostShip(ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse, SerializableDictionary userParams)
{
    // Check for errors
    if (shipmentResponse.PackageDefaults.ErrorCode == 0)
    {
        // Success - access tracking
        string tracking = shipmentResponse.Packages[0].TrackingNumber;
        Money total = shipmentResponse.Packages[0].Total;
    }
    else
    {
        // Error
        string errorMsg = shipmentResponse.Packages[0].ErrorMessage;
    }
}
```

---

### DocumentRequest

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.DocumentRequest`

Passed to `PrePrint` and `Print` SBR hooks. Describes the document to be printed.

| Property | Type | Description |
|----------|------|-------------|
| `Carrier` | `string` | Carrier symbol |
| `Shipper` | `string` | Shipper symbol |
| `DocumentMapping` | `DocumentMapping` | Document mapping configuration |
| `DocumentOptions` | `SerializableDictionary` | Print options as key/value pairs |
| `ItemToPrint` | `int64` | Global MSN or item ID to print |
| `ShipDate` | `Date` | Ship date |
| `ContainerCode` | `string` | Container code |
| `PDocData` | `List<string>` | Pre-rendered document data |

#### DocumentMapping
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Mapping ID |
| `Carrier` | `Carrier` | Associated carrier |
| `CarrierId` | `int` | Carrier ID |
| `Document` | `Document` | Document definition |
| `DocumentId` | `int` | Document ID |
| `DocumentConfigurationId` | `int` | Document config ID |
| `PrinterDefinition` | `PrinterDefinition` | Target printer |
| `PrinterDefinitionId` | `int` | Printer ID |
| `Copies` | `int` | Number of copies |
| `Sequence` | `int` | Print sequence order |
| `PassThrough` | `bool` | Whether to pass through |

#### Usage (SBR - C#)
```csharp
public void PrePrint(DocumentRequest documentRequest, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)
{
    // Suppress a specific document
    if (documentRequest.DocumentMapping.Document.Symbol.Contains("HAZMAT"))
    {
        documentRequest = null; // suppress
    }
}
```

---

### DocumentResponse

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.DocumentResponse`

Returned from print operations and found in `PackageResponse.Documents`.

| Property | Type | Description |
|----------|------|-------------|
| `DocumentName` | `string` | Document name |
| `DocumentSymbol` | `string` | Document symbol (e.g., "TANDATA_COMMERCIAL_INVOICE.STANDARD") |
| `ItemToPrint` | `int64` | Associated item (Global MSN) |
| `ImageData` | `List<string>` | Image data (base64 encoded) |
| `PDocData` | `List<string>` | PDoc format data |
| `PdfData` | `List<string>` | PDF data |
| `RawData` | `List<string>` | Raw printer data |
| `Encoding` | `string` | Data encoding |
| `Copies` | `int` | Number of copies |
| `DocumentDimension` | `DocumentDimension` | Document size |
| `LocalPort` | `string` | Local printer port |

#### DocumentDimension
| Property | Type |
|----------|------|
| `Width` | `double` |
| `Height` | `double` |

---

### PrinterMapping

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.PrinterMapping`

Passed to `PrePrint`, `Print`, and `PostPrint` SBR hooks. Identifies the target printer.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Mapping ID |
| `LocalPort` | `string` | Local port/printer path |
| `PrinterConfigurationId` | `int` | Configuration ID |
| `PrinterDefinition` | `PrinterDefinition` | Full printer definition |
| `PrinterDefinitionId` | `int` | Printer definition ID |

#### PrinterDefinition
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Definition ID |
| `Alias` | `string` | Printer alias name |
| `Url` | `string` | Printer URL/path |
| `CompanyId` | `Guid` | Company GUID |
| `SiteId` | `Guid?` | Site GUID |
| `Model` | `PrinterModel` | Printer model |
| `ModelId` | `int` | Model ID |
| `Stock` | `PrinterStock` | Label stock |
| `StockId` | `int` | Stock ID |
| `ImageType` | `ImageType` (enum) | Output image type |
| `PrintActions` | `PrintActions` (enum) | Print actions |
| `PrintDirection` | `PrintDirection` (enum) | Print direction |
| `PrinterSettings` | `List<PrinterSetting>` | Additional settings |

---

### ManifestItem

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.ManifestItem`

Used in `PreCloseManifest`, `CloseManifest`, and `PostCloseManifest` SBR hooks.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Manifest item ID |
| `Name` | `string` | Manifest name (e.g., "SHIPDATE_20070308") |
| `Symbol` | `string` | Manifest symbol |
| `ShipDate` | `Date` | Ship date for this manifest |
| `Attributes` | `SerializableDictionary` | Additional attributes |

#### CloseManifestResult
| Property | Type | Description |
|----------|------|-------------|
| `Documents` | `List<DocumentResponse>` | Manifest documents |
| `HistoryItem` | `HistoryItem` | History record |
| `IsDatabaseUpdated` | `bool` | Whether DB was updated |
| `ResultData` | `SerializableDictionary` | Additional result data |
| `TransmitItems` | `List<TransmitItem>` | Items ready to transmit |

---

### ModifyPackageListResult

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.ModifyPackageListResult`

Returned from `PostModifyPackageList` SBR hook.

| Property | Type | Description |
|----------|------|-------------|
| `ModifyPackageResults` | `List<ModifyPackageResult>` | Results for each package modified |

#### ModifyPackageResult
| Property | Type | Description |
|----------|------|-------------|
| `Msn` | `int` | Package MSN |
| `Package` | `Package` | Updated package |

---

### TransmitItem

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.TransmitItem`

Used in `PreTransmit`, `Transmit`, and `PostTransmit` SBR hooks.

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Transmit item name |
| `Symbol` | `string` | Transmit item symbol (e.g., "DOM_2_20000310_1_1") |
| `Sequence` | `int` | Sequence number |
| `ShipDate` | `Date` | Ship date |
| `Status` | `int` | Transmit status |
| `FileNames` | `List<string>` | Associated file names |

#### TransmitItemResult
| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Item name |
| `Symbol` | `string` | Item symbol |
| `Status` | `int` | Result status |

---

### PackingRateRequest

**Namespace:** `PSI.Sox.Packing`  
**Full Name:** `PSI.Sox.Packing.PackingRateRequest`

Used in `PrePackRate` SBR hook.

| Property | Type | Description |
|----------|------|-------------|
| `PackageRequest` | `PackageRequest` | The package to pack-rate |
| `RequiredDeliveryDate` | `DateTime` | Required delivery date |
| `Warehouses` | `List<ShippingWarehouse>` | Available warehouses |

---

### PackingRateResponse

**Namespace:** `PSI.Sox.Packing`  
**Full Name:** `PSI.Sox.Packing.PackingRateResponse`

Used in `PostPackRate` SBR hook.

| Property | Type | Description |
|----------|------|-------------|
| `PackRateDetails` | `List<PackingRateDetail>` | Rate details per warehouse/option |

---

### BoxType

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.BoxType`

Used in `Package.BoxType`, `PackageRequest.BoxType`, and `Profile.BoxTypes`.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Box type ID |
| `Name` | `string` | Box type display name |
| `Dimensions` | `Dimensions` | Box dimensions (Length, Width, Height, Units) |
| `Sequence` | `int` | Display order sequence |
| `CompanyId` | `Guid` | Company GUID |
| `SiteId` | `Guid?` | Site GUID (nullable) |

#### Usage (SBR - C#)
```csharp
// Set box type on a package
shipmentRequest.Packages[0].BoxType = new BoxType 
{ 
    Name = "SmallBox", 
    Dimensions = new Dimensions { Length = 12, Width = 10, Height = 8, Units = DimensionUnits.IN } 
};
```

---

### NameAddress

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.NameAddress`

Used for ALL address fields throughout the API: Consignee, OriginAddress, ThirdPartyBillingAddress, ReturnAddress, ImporterOfRecord, HoldAtLocationAddress, etc.

| Property | Type | Description |
|----------|------|-------------|
| `Company` | `string` | Company name |
| `Contact` | `string` | Contact person name |
| `Address1` | `string` | Street address line 1 |
| `Address2` | `string` | Street address line 2 |
| `Address3` | `string` | Street address line 3 |
| `City` | `string` | City |
| `StateProvince` | `string` | State or province code |
| `PostalCode` | `string` | Postal/ZIP code |
| `Country` | `string` | Country code (e.g., "US", "CA", "GB") |
| `Phone` | `string` | Phone number |
| `Fax` | `string` | Fax number |
| `Email` | `string` | Email address |
| `Sms` | `string` | SMS number |
| `Account` | `string` | Account number (for billing addresses) |
| `Code` | `string` | Address code/identifier |
| `Group` | `string` | Address group |
| `TaxId` | `string` | Tax identification number |
| `TaxIdType` | `TaxIdType` (enum) | Type of tax ID |
| `Residential` | `bool` | Whether this is a residential address |
| `PoBox` | `bool` | Whether this is a PO Box |
| `IsValidated` | `bool` | Whether address has been validated |
| `CustomData` | `List<CustomData>` | Custom key/value fields (Custom1–Custom10 = user reference fields) |

#### CustomData Items
`CustomData` is NOT a dictionary — it's a list of objects with `Key` and `Value` (both `string`):
```csharp
// Reading custom data
string dept = address.CustomData.FirstOrDefault(c => c.Key == "Custom1")?.Value ?? "";

// Setting custom data
address.CustomData = new List<CustomData>
{
    new CustomData { Key = "Custom1", Value = "Engineering" },
    new CustomData { Key = "Custom2", Value = "Cost Center 100" }
};
```

#### Usage (SBR - C#)
```csharp
// Set consignee
shipmentRequest.PackageDefaults.Consignee = new NameAddress
{
    Company = "Acme Corp",
    Contact = "John Smith",
    Address1 = "123 Main St",
    City = "Anytown",
    StateProvince = "NY",
    PostalCode = "10001",
    Country = "US",
    Phone = "555-1234",
    Email = "john@acme.com",
    Residential = false
};
```

#### Usage (CBR - JavaScript)
```javascript
// Set consignee in CBR
shipmentRequest.PackageDefaults.Consignee = {
    Company: "Acme Corp",
    Contact: "John Smith",
    Address1: "123 Main St",
    City: "Anytown",
    StateProvince: "NY",
    PostalCode: "10001",
    Country: "US"
};
```

---

### CommodityContent

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.CommodityContent`

Used in `Package.CommodityContents` and `PackageRequest.CommodityContents` (both `List<CommodityContent>`). Represents a single commodity line item for international shipping.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Commodity content ID |
| `Description` | `string` | Commodity description |
| `HarmonizedCode` | `string` | HS/HTS code for customs |
| `ExportHarmonizedCode` | `string` | Export harmonized code |
| `Quantity` | `int64` | Item quantity |
| `QuantityUnitMeasure` | `string` | Quantity unit (e.g., "PCS", "EA") |
| `UnitValue` | `Money` | Value per unit |
| `UnitWeight` | `Weight` | Weight per unit |
| `OriginCountry` | `string` | Country of origin |
| `Origin` | `NameAddress` | Origin address |
| `OriginDescription` | `string` | Origin description |
| `ProductCode` | `string` | Product/part code |
| `ManufacturerId` | `string` | Manufacturer ID |
| `CommodityCondition` | `int` | Condition code |
| `CertOfOriginMethod` | `int` | Certificate of origin method |
| `SedMethod` | `int` | SED method |
| `ExportInformationCode` | `string` | Export information code |
| `ExportQuantity1` | `decimal` | Export quantity 1 |
| `ExportQuantity2` | `decimal` | Export quantity 2 |
| `ExportQuantityUnitMeasure1` | `string` | Export quantity unit 1 |
| `ExportQuantityUnitMeasure2` | `string` | Export quantity unit 2 |
| `LicenseNumber` | `string` | License number |
| `LicenseType` | `string` | License type |
| `LicenseExpirationDate` | `Date` | License expiration |
| `LicenseUnitValue` | `Money` | License unit value |
| `NaftaPreferenceCriterion` | `int` | NAFTA preference |
| `NaftaProducer` | `int` | NAFTA producer |
| `NaftaRvcMethod` | `int` | NAFTA RVC method |
| `NaftaRvcAvgStartDate` | `Date` | NAFTA RVC avg start |
| `NaftaRvcAvgEndDate` | `Date` | NAFTA RVC avg end |
| `RestrictedArticleType` | `int` | Restricted article type |
| `PartiesRelated` | `bool` | Whether parties are related |
| `DdtcRegistrationNumber` | `string` | DDTC registration number |
| `DdtcQuantity` | `int64` | DDTC quantity |
| `DdtcUnitMeasure` | `string` | DDTC unit measure |
| `DdtcUsmlCategoryCode` | `string` | DDTC USML category |
| `DdtcEligibleParty` | `bool` | DDTC eligible party |
| `DdtcSme` | `bool` | DDTC SME flag |
| `ApprovedCommunityMemberNumber` | `string` | Approved community member # |
| `CompanyId` | `Guid` | Company GUID |
| `SiteId` | `Guid?` | Site GUID |
| `EnterpriseId` | `Guid?` | Enterprise GUID |
| `UserData1` through `UserData5` | `object` | Custom user data |

#### Usage (SBR - C#)
```csharp
// Add a commodity
var commodity = new CommodityContent
{
    Description = "Electronic Components",
    HarmonizedCode = "8542.31",
    Quantity = 100,
    QuantityUnitMeasure = "PCS",
    UnitValue = new Money { Amount = 5.50, Currency = "USD" },
    UnitWeight = new Weight { Amount = 0.1, Units = "LB" },
    OriginCountry = "US"
};
shipmentRequest.Packages[0].CommodityContents.Add(commodity);
```

---

### HazmatContent

**Namespace:** `PSI.Sox`  
**Full Name:** `PSI.Sox.HazmatContent`

Used in `Package.HazmatContents` and `PackageRequest.HazmatContents` (both `List<HazmatContent>`). Represents a single hazardous material item.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Hazmat content ID |
| `HazmatId` | `string` | UN/ID number (e.g., "UN1234") |
| `HazmatClass` | `string` | Hazmat class (e.g., "3", "8") |
| `HazmatDescription` | `string` | Proper shipping name |
| `HazmatPacking` | `string` | Packing type |
| `HazmatPackingGroup` | `int` | Packing group (I, II, III) |
| `HazmatPackingInstruction` | `string` | Packing instruction |
| `HazmatLabel` | `int` | Label type |
| `HazmatRegulation` | `int` | Regulation type |
| `HazmatRegulationSet` | `int` | Regulation set |
| `HazmatQuantity` | `HazmatQuantity` | Quantity with units |
| `HazmatReference` | `string` | Reference number |
| `HazmatTechnicalName` | `string` | Technical name |
| `HazmatSubsidiaryRiskClass` | `string` | Subsidiary risk class |
| `HazmatSpecialProvisions` | `string` | Special provisions |
| `HazmatEmergencyContact` | `string` | Emergency contact name |
| `HazmatEmergencyPhone` | `string` | Emergency phone number |
| `HazmatHandlingInformation` | `string` | Handling information |
| `HazmatAccessible` | `bool` | Accessible hazmat |
| `HazmatCargo` | `bool` | Cargo aircraft only |
| `HazmatLimitedQuantity` | `bool` | Limited quantity |
| `HazmatExceptedQuantity` | `bool` | Excepted quantity |
| `HazmatReportableQuantity` | `bool` | Reportable quantity |
| `Hazmat500kgExemption` | `bool` | 500kg exemption |
| `HazmatCaCategory` | `int` | Canadian category |
| `HazmatExNumber` | `string` | Exception number |
| `HazmatPercentage` | `decimal` | Percentage concentration |
| `HazmatInfectiousResponsibleParty` | `NameAddress` | Infectious substance responsible party |
| `HazmatRadioactiveName` | `string` | Radioactive material name |
| `HazmatRadioactiveActivity` | `HazmatQuantity` | Radioactivity level |
| `HazmatRadioactiveChemicalForm` | `string` | Chemical form |
| `HazmatRadioactivePhysicalForm` | `int` | Physical form |
| `HazmatRadioactivePackaging` | `string` | Radioactive packaging |
| `HazmatRadioactiveCsi` | `decimal` | Criticality Safety Index |
| `HazmatRadioactiveTransportIndex` | `decimal` | Transport index |
| `HazmatRadioactiveSurfaceReading` | `HazmatQuantity` | Surface reading |
| `HazmatRadioactiveException` | `int` | Radioactive exception |
| `CompanyId` | `Guid` | Company GUID |
| `SiteId` | `Guid?` | Site GUID |
| `EnterpriseId` | `Guid?` | Enterprise GUID |
| `UserData1` through `UserData5` | `object` | Custom user data |

#### HazmatQuantity
| Property | Type | Description |
|----------|------|-------------|
| `Amount` | `decimal` | Quantity amount |
| `Units` | `string` | Quantity units |

---

### Tools (Template Helper Class)

**Namespace:** `PSI.Sox` (defined in the SBR template project, NOT in the DLL)  
**Full Name:** `PSI.Sox.Tools`

This is a **custom helper class** in the SBR template project (`CodeStandards\TemplateCodeShipExec20BusinessRules\Tools.cs`). It is NOT part of the PSI.Sox.dll — it is user/developer code that ships with the template.

#### Constructor
```csharp
public Tools(ILogger logger)
```

#### Key Methods

| Method | Parameters | Returns | Description |
|--------|-----------|---------|-------------|
| `HasPackageShippedAlready` | `string shipperReference, IBusinessObjectApi api, ClientContext ctx, int daysBack = -10` | `bool` | Searches history for non-voided packages by shipper reference |
| `GetStringValueFromBusinessRuleSettings` | `string key, List<BusinessRuleSetting> settings` | `string` | Looks up a value by key from BusinessRuleSettings |
| `GetStringKeyFromBusinessRuleSettings` | `string value, List<BusinessRuleSetting> settings` | `string` | Reverse lookup: finds key by value |
| `CheckAndSetPaperless` | `ShipmentRequest req, IBusinessObjectApi api, ClientContext ctx, SerializableDictionary userParams` | `void` | Checks and sets paperless commercial invoice for international |
| `SuppressPrintingCommericalInvoice` | `ShipmentRequest req, ShipmentResponse resp` | `ShipmentResponse` | Removes commercial invoice documents from response |
| `CheckDHLShipments` | `ShipmentRequest req, List<Shipper> shippers` | `void` | Validates DHL doesn't ship domestically |
| `SuppressPrintingHazMatDocument` | `DocumentRequest doc, Package pkg` | `DocumentRequest` | Returns null if hazmat doc should be suppressed |
| `CleanUpBatchRecords` | `IBusinessObjectApi api, ClientContext ctx, List<BusinessRuleSetting> settings` | `void` | Deletes old batch records based on settings |
| `GetCarrierSymbolFromServiceSymbol` | `string serviceSymbol` | `string` | Extracts carrier symbol from service symbol |
| `IsDimensionsFormatCorrect` | `string text` | `bool` | Validates dimensions format (nnn.nnXnnn.nnXnnn.nn) |
| `IsPoBoxFound` | `NameAddress consignee` | `bool` | Checks if address contains a PO Box |
| `IsValidUSPhone` | `string phone` | `bool` | Validates US phone number format |
| `ConvertToBool` | `string value` | `bool` | Converts various string values to bool |
| `RemoveNullsReturnEmptyString` | `object value` | `string` | Returns trimmed string or empty if null |
| `IsEmailFormatValid` | `string emailAddress` | `bool` | Validates email format |
| `IsNumeric` | `string text` | `bool` | Checks if string is numeric |
| `FromCsv` | `string line` | `CommodityContent` | Parses a CSV line into a CommodityContent |

#### Usage (SBR - C#)
```csharp
public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
{
    Tools tools = new Tools(Logger);

    // Check if already shipped
    if (tools.HasPackageShippedAlready(
        shipmentRequest.Packages[0].ShipperReference, 
        BusinessObjectApi, ClientContext))
    {
        throw new Exception("This package has already been shipped.");
    }

    // Get setting value
    string connStr = tools.GetStringValueFromBusinessRuleSettings("ConnectionString", BusinessRuleSettings);

    // Check DHL
    tools.CheckDHLShipments(shipmentRequest, Profile.Shippers.ToList());
}
```

---

### IBusinessObjectApi (Interface)

**Namespace:** `PSI.Sox.Interfaces`  
**Available as:** `this.BusinessObjectApi` in SBR class

Key methods available for calling ShipExec operations from within SBR code:

| Method | Description |
|--------|-------------|
| `Ship(...)` | Ship a package |
| `Rate(...)` | Rate a shipment |
| `VoidPackages(...)` | Void packages |
| `SearchPackageHistory(...)` | Search shipped package history |
| `GetHistoryItems(...)` | Get history items |
| `PrintDocument(...)` | Print a document |
| `GetDocuments(...)` | Get available documents |
| `ValidateNameAddress(...)` | Validate an address |
| `GetClientProfile(...)` | Get user's client profile |
| `GetAddressBookEntries(...)` | Get address book entries |
| `AddAddressBookEntry(...)` | Add address book entry |
| `GetBatches(...)` | Get batch list |
| `GetBatchItems(...)` | Get batch items |
| `ProcessBatch(...)` | Process a batch |
| `RemoveBatch(...)` | Remove a batch |
| `GetManifestItems(...)` | Get manifest items |
| `CloseManifest(...)` | Close a manifest |
| `Transmit(...)` | Transmit data |
| `GetTransmitItems(...)` | Get transmit items |
| `GetGroups(...)` | Get groups |
| `CreateGroup(...)` | Create a group |
| `ModifyGroup(...)` | Modify a group |
| `CloseGroup(...)` | Close a group |
| `OpenGroup(...)` | Open a group |
| `ModifyPackageList(...)` | Modify package list |
| `Pack(...)` | Pack operation |
| `PackRate(...)` | Pack rate operation |
| `CreatePickup(...)` | Create a pickup |
| `CancelPickup(...)` | Cancel a pickup |
| `Track(...)` | Track a package |
| `ReProcess(...)` | Reprocess packages |
| `GetPendingShipments(...)` | Get pending shipments |
| `AddPendingShipment(...)` | Add pending shipment |
| `UpdatePendingShipment(...)` | Update pending shipment |
| `RemovePendingShipments(...)` | Remove pending shipments |
| `GetSbrCustomData(...)` | Get SBR custom data |
| `AddSbrCustomData(...)` | Add SBR custom data |
| `UpdateSbrCustomData(...)` | Update SBR custom data |
| `RemoveSbrCustomData(...)` | Remove SBR custom data |
| `RetrieveData(...)` | Retrieve data |
| `UploadData(...)` | Upload data |

---

### Inheritance Hierarchy (Error Handling)

Understanding the error base classes is critical:

```
ErrorBase (abstract)
├── ErrorCode : int
├── ErrorMessage : string
├── PackageRequest (extends ErrorBase)
├── PackageResponse (extends ErrorBase)
├── BatchItem (extends ErrorBase)
└── ...

CompoundErrorBase (extends ErrorBase)
├── Errors : List<CompoundError>
├── Package (extends CompoundErrorBase)
└── ...
```

**Key Rule:** Always check `ErrorCode == 0` for success on response objects.

---

## Common Patterns & Pitfalls

### ⚠️ Type Assignment Mistakes to Avoid

1. **`Service` is an object, NOT a string:**
   ```csharp
   // WRONG:
   shipmentRequest.PackageDefaults.Service = "UPS Ground";

   // CORRECT:
   shipmentRequest.PackageDefaults.Service = new Service { Symbol = "CONNECTSHIP_UPS.UPS.GND", Name = "UPS Ground" };
   ```

2. **`PackageExtras` is a `SerializableDictionary` (Dictionary<string, object>), NOT a list:**
   ```csharp
   // WRONG:
   package.PackageExtras.Add(new CustomData { Key = "x", Value = "y" });

   // CORRECT:
   package.PackageExtras["x"] = "y";
   ```

3. **`ShipmentRequest.Packages` contains `PackageRequest` objects, NOT `Package` objects:**
   ```csharp
   // The Packages list is List<PackageRequest>, not List<Package>
   PackageRequest pkg = shipmentRequest.Packages[0];
   ```

4. **`ShipmentRequest.PackageDefaults` IS a `Package` object (not `PackageRequest`):**
   ```csharp
   Package defaults = shipmentRequest.PackageDefaults;
   ```

5. **`Weight` and `Money` are objects with `Amount` properties:**
   ```csharp
   // WRONG:
   package.Weight = 5.0;

   // CORRECT:
   package.Weight = new Weight { Amount = 5.0, Units = "LB" };
   ```

6. **`CustomData` is a `List<CustomData>` on `NameAddress`, NOT a dictionary:**
   ```csharp
   // Access custom fields by iterating
   var custom1 = address.CustomData.FirstOrDefault(c => c.Key == "Custom1")?.Value;
   ```

7. **`Shipper` (on Package) is a `string` (the shipper symbol), but `Profile.Shippers` is `List<Shipper>`:**
   ```csharp
   // On Package, it's just the symbol string
   shipmentRequest.PackageDefaults.Shipper = "MYSHIPPER";  // This is the string property
   ```

8. **`Terms` does not exist on Package — use the shipper symbol field or `ThirdPartyBilling`:**
   ```javascript
   // In CBR JavaScript, "Terms" is set as a string
   shipmentRequest.PackageDefaults.Terms = "SHIPPER";  // CBR-only shortcut
   ```
