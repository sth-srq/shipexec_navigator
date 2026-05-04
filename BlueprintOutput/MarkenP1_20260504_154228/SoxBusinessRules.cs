using System;
using System.Collections.Generic;
using System.Globalization;
using PSI.Sox.Interfaces;
using PSI.Sox.Packing;

namespace PSI.Sox
{
    [BusinessRuleMetadata(Author = "Chris Phillips", AuthorEmail = "cle5cap@ups.com", CompanyId = "0be765a7-52f4-4eff-8ade-84c22b219365", Description = "Template Code ShipExec 2 Business Rules", Name = "TemplateCodeSBR", Version = "1")]
    public class SoxBusinessRules : IBusinessObject
    {
        public IBusinessObjectApi BusinessObjectApi { get; set; }
        public ILogger Logger { get; set; }
        public IProfile Profile { get; set; }
        public List<BusinessRuleSetting> BusinessRuleSettings { get; set; }
        public ClientContext ClientContext { get; set; }

        public ShipmentRequest Load(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            return shipmentRequest;
        }

        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            try
            {
                Logger.Log(this, LogLevel.Info, "SBR PreShip: start biological returns processing.");

                if (shipmentRequest == null)
                    throw new Exception("Shipment request is missing.");

                if (shipmentRequest.PackageDefaults == null)
                    shipmentRequest.PackageDefaults = new Package();

                if (shipmentRequest.PackageDefaults.Consignee == null)
                    shipmentRequest.PackageDefaults.Consignee = new NameAddress();

                if (shipmentRequest.PackageDefaults.Shipper == null)
                    shipmentRequest.PackageDefaults.Shipper = new NameAddress();

                if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
                    shipmentRequest.Packages = new List<PackageRequest> { new PackageRequest() };

                if (shipmentRequest.Packages[0].Weight == null)
                    shipmentRequest.Packages[0].Weight = new Weight();

                bool isBiologicalSample = string.Equals(shipmentRequest.PackageDefaults.MiscReference4, "true", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(shipmentRequest.PackageDefaults.MiscReference4, "1", StringComparison.OrdinalIgnoreCase) ||
                                          string.Equals(shipmentRequest.PackageDefaults.MiscReference4, "yes", StringComparison.OrdinalIgnoreCase);

                string shipperCountry = shipmentRequest.PackageDefaults.Shipper.Country == null ? string.Empty : shipmentRequest.PackageDefaults.Shipper.Country.Trim().ToUpperInvariant();
                string consigneeCountry = shipmentRequest.PackageDefaults.Consignee.Country == null ? string.Empty : shipmentRequest.PackageDefaults.Consignee.Country.Trim().ToUpperInvariant();
                bool isDomesticUS = shipperCountry == "US" && consigneeCountry == "US";
                bool isInternational = !string.IsNullOrWhiteSpace(shipperCountry) && !string.IsNullOrWhiteSpace(consigneeCountry) && shipperCountry != consigneeCountry;

                if (isInternational)
                {
                    shipmentRequest.PackageDefaults.CommercialInvoiceMethod = 1;
                    shipmentRequest.PackageDefaults.ExportReason = "Medical";
                    Logger.Log(this, LogLevel.Info, "SBR PreShip: applied paperless invoice and medical export reason for international return shipment.");
                }

                if (isBiologicalSample)
                {
                    if (shipmentRequest.Packages[0].PackageExtras == null)
                        shipmentRequest.Packages[0].PackageExtras = new List<object>();

                    Logger.Log(this, LogLevel.Info, "SBR PreShip: biological sample detected.");
                }

                string dryIceKgText = shipmentRequest.PackageDefaults.MiscReference3;
                if (!string.IsNullOrWhiteSpace(dryIceKgText))
                {
                    if (!decimal.TryParse(dryIceKgText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dryIceKg))
                        throw new Exception("Dry Ice Weight (MiscReference3) must be numeric.");

                    decimal dryIceLbs = Math.Round(dryIceKg * 2.2046226218m, 2, MidpointRounding.AwayFromZero);
                    shipmentRequest.Packages[0].Weight.Amount += (double)dryIceLbs;
                    shipmentRequest.Packages[0].DryIceWeight = (double)dryIceLbs;
                    shipmentRequest.Packages[0].DryIcePurpose = "Medical";
                    shipmentRequest.Packages[0].DryIceRegulationSet = isDomesticUS
                        ? "US 49 CFR regulations."
                        : "International Air Transportation Association regulations.";

                    Logger.Log(this, LogLevel.Info, $"SBR PreShip: applied dry ice weight {dryIceKg} KG ({dryIceLbs} LBS), purpose Medical, and regulation set.");
                }

                if (GetBoolSetting("EnablePickupBackupStrategy", false))
                {
                    EnsurePickupFromCustomData(shipmentRequest);
                    Logger.Log(this, LogLevel.Info, "SBR PreShip: pickup backup strategy enabled and evaluated.");
                }

                Logger.Log(this, LogLevel.Info, "SBR PreShip: completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log(this, LogLevel.Error, "SBR PreShip failed. " + ex.Message);
                throw;
            }
        }

        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            try
            {
                Logger.Log(this, LogLevel.Info, "SBR Ship: start.");

                if (GetBoolSetting("EnablePickupBackupStrategy", false))
                {
                    EnsurePickupFromCustomData(shipmentRequest);
                    Logger.Log(this, LogLevel.Info, "SBR Ship: evaluated pickup backup strategy.");
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Log(this, LogLevel.Error, "SBR Ship failed. " + ex.Message);
                throw;
            }
        }

        public void PostShip(ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse, SerializableDictionary userParams)
        {
            if (shipmentResponse.PackageDefaults.ErrorCode == 0)
            {
                Tools tools = new Tools(Logger);
                shipmentResponse = tools.SuppressPrintingCommericalInvoice(shipmentRequest, shipmentResponse);
            }
        }

        public void PreReprocess(string carrier, List<long> globalMsns, SerializableDictionary userParams) { }
        public void PostReprocess(string carrier, List<long> globalMsns, ReProcessResult reProcessResponse, SerializableDictionary userParams) { }
        public void PrePrint(DocumentRequest documentRequest, PrinterMapping printerMapping, Package package, SerializableDictionary userParams) { }
        public DocumentResponse Print(DocumentRequest document, PrinterMapping printerMapping, Package package, SerializableDictionary userParams) { return null; }
        public void PostPrint(DocumentRequest document, DocumentResponse documentResponse, PrinterMapping printerMapping, Package package, SerializableDictionary userParams) { }
        public string ErrorLabel(Package package, SerializableDictionary userParams) { return null; }
        public void PreRate(ShipmentRequest shipmentRequest, List<Service> services, SortType sortType, SerializableDictionary userParams) { }
        public List<ShipmentResponse> Rate(ShipmentRequest shipmentRequest, List<Service> services, SortType sortType, SerializableDictionary userParams) { return null; }
        public void PostRate(ShipmentRequest shipmentRequest, List<ShipmentResponse> shipmentResponses, List<Service> services, SortType sortType, SerializableDictionary userParams) { }
        public void PreCloseManifest(string carrier, string shipper, ManifestItem manifestItem, SerializableDictionary userParams) { }
        public CloseManifestResult CloseManifest(string carrier, string shipper, ManifestItem manifestItem, bool print, SerializableDictionary userParams) { return null; }
        public void PostCloseManifest(string carrier, string shipper, ManifestItem manifestItem, CloseManifestResult closeOutResult, List<Package> packages, SerializableDictionary userParams) { }
        public void PreVoid(Package package, SerializableDictionary userParams) { }
        public Package VoidPackage(Package package, SerializableDictionary userParams) { return package; }
        public void PostVoid(Package package, SerializableDictionary userParams) { }
        public void PreCloseGroup(string carrier, string groupType, SerializableDictionary userParams) { }
        public void PostCloseGroup(string carrier, string groupType, Group group, SerializableDictionary userParams) { }
        public void PreCreateGroup(string carrier, string groupType, PackageRequest packageRequest, SerializableDictionary userParams) { }
        public void PostCreateGroup(string carrier, string groupType, Group group, PackageRequest packageRequest, SerializableDictionary userParams) { }
        public void PreModifyGroup(string carrier, long groupId, string groupType, PackageRequest packageRequest, SerializableDictionary userParams) { }
        public void PostModifyGroup(string carrier, Group group, string groupType, SerializableDictionary userParams) { }
        public void PreModifyPackageList(string carrier, List<long> globalMsns, Package package, SerializableDictionary userParams) { }
        public void PostModifyPackageList(string carrier, ModifyPackageListResult modifyPackageListResult, Package package, SerializableDictionary userParams) { }
        public void PreTransmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams) { }
        public List<TransmitItemResult> Transmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams) { return null; }
        public void PostTransmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams) { }
        public List<BatchReference> GetBatchReferences(SerializableDictionary userParams) { return null; }
        public BatchRequest LoadBatch(string batchReference, SerializableDictionary userParams) { return null; }
        public BatchRequest ParseBatchFile(string batchReference, System.IO.Stream fileStream, SerializableDictionary userParams) { return null; }
        public void PreProcessBatch(BatchRequest batchRequest, ProcessBatchActions batchActions, SerializableDictionary userParams) { }
        public void PostProcessBatch(BatchRequest batchRequest, ProcessBatchActions batchActions, ProcessBatchResult processBatchResult, SerializableDictionary userParams) { }
        public void PrePackRate(PackingRateRequest packingRateRequest, SerializableDictionary userParams) { }
        public void PostPackRate(PackingRateRequest packingRateRequest, PackingRateResponse packingRateResponse, SerializableDictionary userParams) { }
        public void PrePack(PackingRequest packingRequest, SerializableDictionary userParams) { }
        public void PostPack(PackingRequest packingRequest, PackingResponse packingResponse, SerializableDictionary userParams) { }
        public void PreAddressValidation(NameAddress nameAddress, bool useSimpleNameAddress, SerializableDictionary userParams) { }
        public List<NameAddressValidationCandidate> AddressValidation(NameAddress nameAddress, bool useSimpleNameAddress, SerializableDictionary userParams) { return null; }
        public void PostAddressValidation(NameAddress nameAddress, List<NameAddressValidationCandidate> addressValidationCandidates, SerializableDictionary userParams) { }
        public List<BoxType> GetBoxTypes(List<BoxType> definedBoxTypes) { return null; }
        public List<ShipmentRequest> LoadDistributionList(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams) { return null; }
        public object UserMethod(object userObject) { return null; }
        public List<CommodityContent> GetCommodityContents(List<CommodityContent> definedCommodityContents) { return null; }
        public List<HazmatContent> GetHazmatContents(List<HazmatContent> definedHazmatContents) { return null; }

        private bool GetBoolSetting(string key, bool defaultValue)
        {
            try
            {
                if (BusinessRuleSettings == null)
                    return defaultValue;

                var setting = BusinessRuleSettings.Find(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
                    return defaultValue;

                bool parsed;
                return bool.TryParse(setting.Value, out parsed) ? parsed : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private void EnsurePickupFromCustomData(ShipmentRequest request)
        {
        }
    }
}
