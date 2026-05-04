using System.Collections.Generic;
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
        public IBusinessObjectApi BusinessObjectApi { get; set; }
        public ILogger Logger { get; set; }
        public IProfile Profile { get; set; }
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
            var mgr = new ReturnsShipmentManager(Logger, BusinessObjectApi, Profile, BusinessRuleSettings, ClientContext); mgr.PreShip(shipmentRequest, userParams);
        }

        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            var mgr = new ReturnsShipmentManager(Logger, BusinessObjectApi, Profile, BusinessRuleSettings, ClientContext); return mgr.Ship(shipmentRequest, pickup, shipWithoutTransaction, print, userParams);
        }

        public void PostShip(ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse, SerializableDictionary userParams)
        {
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
