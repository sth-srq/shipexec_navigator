using System;
using System.Collections.Generic;
using System.Linq;
using PSI.Sox.Interfaces;
using PSI.Sox.Packing;

namespace PSI.Sox
{
    /// <summary>
    /// Sox Business Rules Object 
    /// </summary>
    /// <remarks>This class requires a reference to PSI.Sox.dll</remarks>
    /// <remarks>The connection_strings property has been removed. Connection strings are now accessible through the management layer.</remarks>
    [BusinessRuleMetadata(Author = "Chris Phillips", AuthorEmail = "cle5cap@ups.com", CompanyId = "0be765a7-52f4-4eff-8ade-84c22b219365", Description = "Template Code ShipExec 2 Business Rules", Name = "TemplateCodeSBR", Version = "1")]
    public class SoxBusinessRules : IBusinessObject
    {
        #region Properties
        /// <summary>
        /// Gets or set the management layer object
        /// </summary>
        /// <remarks>This property is populated when the SoxBusinessRules class is implemented by the management
        /// layer and contains an instance of IManagmentLayer.</remarks>
        public IBusinessObjectApi BusinessObjectApi { get; set; }

        /// <summary>
        /// Gets or set Logger Object
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Profile for a given user context.
        /// </summary>
        public IProfile Profile { get; set; }

        /// <summary>
        ///     BusinessRuleSettings
        /// </summary>
        public List<BusinessRuleSetting> BusinessRuleSettings { get; set; }

        public ClientContext ClientContext { get; set; }
        #endregion

        #region Load
        public ShipmentRequest Load(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            return shipmentRequest;
        }
        #endregion Load

        #region Ship Rules
        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
            {
                throw new Exception("Shipment request is required.");
            }

            if (shipmentRequest.PackageDefaults == null)
            {
                shipmentRequest.PackageDefaults = new ShipmentPackageDefaults();
            }

            if (shipmentRequest.PackageDefaults.Consignee == null)
            {
                shipmentRequest.PackageDefaults.Consignee = new NameAddress();
            }

            if (shipmentRequest.PackageDefaults.Shipper == null)
            {
                shipmentRequest.PackageDefaults.Shipper = string.Empty;
            }

            if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
            {
                shipmentRequest.Packages = new List<PackageRequest> { new PackageRequest() };
            }

            PackageRequest packageRequest = shipmentRequest.Packages[0];
            if (packageRequest == null)
            {
                packageRequest = new PackageRequest();
                shipmentRequest.Packages[0] = packageRequest;
            }

            string consigneeCountry = shipmentRequest.PackageDefaults.Consignee.Country;
            string normalizedConsigneeCountry = string.IsNullOrWhiteSpace(consigneeCountry) ? string.Empty : consigneeCountry.Trim();
            bool isInternational = !string.IsNullOrEmpty(normalizedConsigneeCountry) && !string.Equals(normalizedConsigneeCountry, "US", StringComparison.OrdinalIgnoreCase);
            bool isUSToUS = string.Equals(normalizedConsigneeCountry, "US", StringComparison.OrdinalIgnoreCase);

            bool biologicalSample = false;
            object bioValue = shipmentRequest.PackageDefaults.MiscReference4;
            if (bioValue != null)
            {
                bool.TryParse(bioValue.ToString(), out biologicalSample);
            }

            if (isInternational)
            {
                packageRequest.CommercialInvoiceMethod = 1;
                packageRequest.ExportReason = "Medical";
            }

            if (biologicalSample)
            {
                if (packageRequest.PackageExtras == null)
                {
                    packageRequest.PackageExtras = new List<KeyValuePair<string, object>>();
                }

                if (!packageRequest.PackageExtras.Any(x => string.Equals(x.Key, "RESTRICTED_ARTICLE_TYPE", StringComparison.OrdinalIgnoreCase)))
                {
                    packageRequest.PackageExtras.Add(new KeyValuePair<string, object>("RESTRICTED_ARTICLE_TYPE", "32"));
                }
            }

            string dryIceWeightText = shipmentRequest.PackageDefaults.MiscReference3;
            decimal dryIceKg = 0m;
            bool hasDryIce = !string.IsNullOrWhiteSpace(dryIceWeightText) && decimal.TryParse(dryIceWeightText, out dryIceKg) && dryIceKg > 0;

            if (hasDryIce)
            {
                decimal dryIceLbs = decimal.Round(dryIceKg * 2.2046226218m, 2, MidpointRounding.AwayFromZero);

                if (packageRequest.Weight == null)
                {
                    packageRequest.Weight = new Weight();
                }

                packageRequest.Weight.Amount = packageRequest.Weight.Amount + (double)dryIceLbs;
                shipmentRequest.PackageDefaults.DryIceWeight = dryIceLbs.ToString("0.##");
                shipmentRequest.PackageDefaults.DryIcePurpose = "Medical";
                shipmentRequest.PackageDefaults.DryIceRegulationSet = isUSToUS ? "International Air Transportation Association regulations." : "US 49 CFR regulations.";
            }

            if (isUSToUS)
            {
                shipmentRequest.PackageDefaults.Service = new Service { Symbol = "NDA_Early_AM" };
                shipmentRequest.PackageDefaults.SaturdayDelivery = false;
            }
            else
            {
                shipmentRequest.PackageDefaults.Service = new Service { Symbol = "UPS_Express_Saturday" };
                shipmentRequest.PackageDefaults.SaturdayDelivery = true;
            }
        }

        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
            {
                throw new Exception("Shipment request is required.");
            }

            return null;
        }

        public void PostShip(ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse, SerializableDictionary userParams)
        {
            if (shipmentResponse != null && shipmentResponse.PackageDefaults != null && shipmentResponse.PackageDefaults.ErrorCode == 0)
            {
                Tools tools = new Tools(Logger);
                shipmentResponse = tools.SuppressPrintingCommericalInvoice(shipmentRequest, shipmentResponse);
            }
        }
        #endregion

        #region Reprocess Rules
        public void PreReprocess(string carrier, List<long> globalMsns, SerializableDictionary userParams)
        {

        }

        public void PostReprocess(string carrier, List<long> globalMsns, ReProcessResult reProcessResponse, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Print Rules
        public void PrePrint(DocumentRequest documentRequest, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)
        {

        }

        public DocumentResponse Print(DocumentRequest document, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)
        {
            return null;
        }

        public void PostPrint(DocumentRequest document, DocumentResponse documentResponse, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)
        {

        }

        public string ErrorLabel(Package package, SerializableDictionary userParams)
        {
            return null;
        }
        #endregion

        #region Rate Rules 
        public void PreRate(ShipmentRequest shipmentRequest, List<Service> services, SortType sortType, SerializableDictionary userParams)
        {

        }

        public List<ShipmentResponse> Rate(ShipmentRequest shipmentRequest, List<Service> services, SortType sortType, SerializableDictionary userParams)
        {
            return null;
        }

        public void PostRate(ShipmentRequest shipmentRequest, List<ShipmentResponse> shipmentResponses, List<Service> services, SortType sortType, SerializableDictionary userParams)
        {

        }
        #endregion

        #region CloseManifest Rules
        public void PreCloseManifest(string carrier, string shipper, ManifestItem manifestItem, SerializableDictionary userParams)
        {

        }

        public CloseManifestResult CloseManifest(string carrier, string shipper, ManifestItem manifestItem, bool print, SerializableDictionary userParams)
        {
            return null;
        }
        public void PostCloseManifest(string carrier, string shipper, ManifestItem manifestItem, CloseManifestResult closeOutResult, List<Package> packages, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Void Rules
        public void PreVoid(Package package, SerializableDictionary userParams)
        {

        }

        public Package VoidPackage(Package package, SerializableDictionary userParams)
        {
            return package;
        }

        public void PostVoid(Package package, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Group Rules
        public void PreCloseGroup(string carrier, string groupType, SerializableDictionary userParams)
        {

        }

        public void PostCloseGroup(string carrier, string groupType, Group group, SerializableDictionary userParams)
        {

        }

        public void PreCreateGroup(string carrier, string groupType, PackageRequest packageRequest, SerializableDictionary userParams)
        {

        }

        public void PostCreateGroup(string carrier, string groupType, Group group, PackageRequest packageRequest, SerializableDictionary userParams)
        {

        }

        public void PreModifyGroup(string carrier, long groupId, string groupType, PackageRequest packageRequest, SerializableDictionary userParams)
        {

        }

        public void PostModifyGroup(string carrier, Group group, string groupType, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Modify Package List Rules
        public void PreModifyPackageList(string carrier, List<long> globalMsns, Package package, SerializableDictionary userParams)
        {

        }

        public void PostModifyPackageList(string carrier, ModifyPackageListResult modifyPackageListResult, Package package, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Transmit Rules
        public void PreTransmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)
        {

        }

        public List<TransmitItemResult> Transmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)
        {
            return null;
        }

        public void PostTransmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Batch
        public List<BatchReference> GetBatchReferences(SerializableDictionary userParams)
        {
            return null;
        }

        public BatchRequest LoadBatch(string batchReference, SerializableDictionary userParams)
        {
            return null;
        }

        public BatchRequest ParseBatchFile(string batchReference, System.IO.Stream fileStream, SerializableDictionary userParams)
        {
            return null;
        }

        public void PreProcessBatch(BatchRequest batchRequest, ProcessBatchActions batchActions, SerializableDictionary userParams)
        {

        }

        public void PostProcessBatch(BatchRequest batchRequest, ProcessBatchActions batchActions, ProcessBatchResult processBatchResult, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Pack
        public void PrePackRate(PackingRateRequest packingRateRequest, SerializableDictionary userParams)
        {

        }

        public void PostPackRate(PackingRateRequest packingRateRequest, PackingRateResponse packingRateResponse, SerializableDictionary userParams)
        {

        }

        public void PrePack(PackingRequest packingRequest, SerializableDictionary userParams)
        {

        }

        public void PostPack(PackingRequest packingRequest, PackingResponse packingResponse, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Address Validation
        public void PreAddressValidation(NameAddress nameAddress, bool useSimpleNameAddress, SerializableDictionary userParams)
        {

        }

        public List<NameAddressValidationCandidate> AddressValidation(NameAddress nameAddress, bool useSimpleNameAddress, SerializableDictionary userParams)
        {
            return null;
        }

        public void PostAddressValidation(NameAddress nameAddress, List<NameAddressValidationCandidate> addressValidationCandidates, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Misc
        public List<BoxType> GetBoxTypes(List<BoxType> definedBoxTypes)
        {
            return null;
        }

        public List<ShipmentRequest> LoadDistributionList(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            return null;
        }

        public object UserMethod(object userObject)
        {
            return null;
        }

        public List<CommodityContent> GetCommodityContents(List<CommodityContent> definedCommodityContents)
        {
            return null;
        }

        public List<HazmatContent> GetHazmatContents(List<HazmatContent> definedHazmatContents)
        {
            return null;
        }
        #endregion
    }
}