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
        /// <summary>
        /// Business rules hook that returns a ShipmentRequest object.
        /// </summary>
        /// <param name="value">String value containing an order number, etc.</param>
        /// <param name="shipmentRequest">ShipmentRequest object passed in containing the current shipment information if called from a ShipExec Client.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        /// <returns>ShipmentRequest object.</returns>        
        public ShipmentRequest Load(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            // Load
            //if (string.IsNullOrEmpty(value))
            //{
            //    throw new Exception("You did not input a valid ordernumber/scancode.");
            //}
            //else
            //{
            //    LoadShipment loadShipment = new LoadShipment(Logger);
            //
            //    shipmentRequest = loadShipment.GetShipmentRequest(value, shipmentRequest, userParams, BusinessRuleSettings);
            //}
        
            return shipmentRequest;
        }
        #endregion Load

        #region Ship Rules
        /// <summary>
        /// Business rules hook fires before the Ship event.
        /// </summary>        
        /// <param name="shipmentRequest">ShipmentRequest object containing the current shipment information.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            var mgr = new ReturnShipmentManager(Logger, BusinessObjectApi, BusinessRuleSettings, Profile, ClientContext);
            mgr.PreShip(shipmentRequest, userParams);
        }

        /// <summary>
        /// Business rules hook that can be used to override the Ship event.
        /// </summary>        
        /// <param name="shipmentRequest">ShipmentRequest object containing the current shipment information.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            var mgr = new ReturnShipmentManager(Logger, BusinessObjectApi, BusinessRuleSettings, Profile, ClientContext);
            return mgr.Ship(shipmentRequest, pickup, shipWithoutTransaction, print, userParams);
        }

        /// <summary>
        /// Business rules hook that fires after the Ship event.
        /// </summary>        
        /// <param name="shipmentRequest">ShipmentRequest object containing the shipment that was shipped.</param>
        /// <param name="shipmentResponse">ShipmentResponse object containing the Ship event result.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostShip(ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse, SerializableDictionary userParams)
        {
            if (shipmentResponse.PackageDefaults.ErrorCode == 0)
            {
                Tools Tools = new Tools(Logger);
                
                shipmentResponse = Tools.SuppressPrintingCommericalInvoice(shipmentRequest, shipmentResponse);
            }
        }
        #endregion

        #region Reprocess Rules
        ///<summary>
        /// Business rules hook that fires before the Reprocess event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="globalMsns">Global Msns.</param>
        /// <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreReprocess(string carrier, List<long> globalMsns, SerializableDictionary userParams)
        {

        }

        ///<summary>
        /// Business rules hook that fires after the Reprocess event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="globalMsns">Global Msns.</param>
        /// <param name="reProcessResponse">ReProcess Response.</param>
        /// <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostReprocess(string carrier, List<long> globalMsns, ReProcessResult reProcessResponse, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Print Rules
        /// <summary>
        /// Business rules hook that fires before the Print event.
        /// </summary>
        /// <param name="documentRequest">SoxDocument object containing the document information to print.</param>
        /// <param name="printerMapping">Printer object containg the printer to print to.</param>
        /// <param name="package">Package object containg the package information for the document being printed.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PrePrint(DocumentRequest documentRequest, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that can be used to override the Print event.
        /// </summary>
        /// <param name="document">SoxDocument object containing the document information to print.</param>
        /// <param name="printerMapping">Printer object containg the printer to print to.</param>
        /// <param name="package">Package object containg the package information for the document being printed.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public DocumentResponse Print(DocumentRequest document, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook that fires after the Print event.
        /// </summary>
        /// <param name="document">SoxDocument object containing the document that was printed.</param>
        /// <param name="documentResponse"></param>
        /// <param name="printerMapping">Printer object containg the printer the document was sent to.</param>
        /// <param name="package">Package object containg the package information for the document that was printed.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostPrint(DocumentRequest document, DocumentResponse documentResponse, PrinterMapping printerMapping, Package package, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that fires when an error occurs while processing a shipment.
        /// </summary>
        /// <param name="package">Package object containing the current package.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        /// <remarks>This method must return a pdoc.</remarks>
        public string ErrorLabel(Package package, SerializableDictionary userParams)
        {
            return null;
        }
        #endregion

        #region Rate Rules 
        /// <summary>
        /// Business rules hook that fires before the Rate event.
        /// </summary>
        /// <param name="shipmentRequest">ShipmentRequest object containg the shipment and package information that needs to be rated.</param>
        /// <param name="services">List of Service Symbols to rate. Example: PSI.UPS.GND</param>
        /// <param name="sortType">Contains the int value for the sort order of the returned list of ShipmentResponse objects.
        /// No Order-- 0, Lowest Rate-- 1, Earliest Commitment-- 2</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreRate(ShipmentRequest shipmentRequest, List<Service> services, SortType sortType, SerializableDictionary userParams)
        {

        }

        /// <inheritdoc />
        public List<ShipmentResponse> Rate(ShipmentRequest shipmentRequest, List<Service> services, SortType sortType, SerializableDictionary userParams)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook that fires after the Rate event.
        /// </summary>
        /// <param name="shipmentRequest">ShipmentRequest object containg the shipment and package information that was rated.</param>
        /// <param name="services">List of Service Symbols to rate. Example: PSI.UPS.GND</param>
        /// <param name="sortType">Contains the int value for the sort order of the returned list of ShipmentResponse objects.
        /// No Order-- 0, Lowest Rate-- 1, Earliest Commitment-- 2</param>
        /// <param name="shipmentResponses">List of ShipmentResponse objects containing the Rate event results.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostRate(ShipmentRequest shipmentRequest, List<ShipmentResponse> shipmentResponses, List<Service> services, SortType sortType, SerializableDictionary userParams)
        {

        }
        #endregion

        #region CloseManifest Rules
        /// <summary>
        /// Business rules hook that fires before the CloseManifest event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="shipper">ShipExec Shipper Symbol. Example: TEX</param>
        /// <param name="manifestItem">CloseManifest Item</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreCloseManifest(string carrier, string shipper, ManifestItem manifestItem, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that can be used to override the CloseManifest event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="shipper">ShipExec Shipper Symbol. Example: TEX</param>
        /// <param name="manifestItem">CloseManifest Item Symbol. Example: SHIPDATE_20070308</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public CloseManifestResult CloseManifest(string carrier, string shipper, ManifestItem manifestItem, bool print, SerializableDictionary userParams)
        {
            return null;
        }
        /// <summary>
        /// Business rules hook that fires after CloseManifest event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="shipper">ShipExec Shipper Symbol. Example: TEX</param>
        /// <param name="manifestItem">CloseManifest Item Symbol. Example: SHIPDATE_20070308</param>
        /// <param name="closeOutResult">CloseOutResult object containing the result of the closeout operation.</param>
        /// <param name="packages">List of Package objects containing the package(s) that were closed out.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostCloseManifest(string carrier, string shipper, ManifestItem manifestItem, CloseManifestResult closeOutResult, List<Package> packages, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Void Rules
        /// <summary>
        /// Business rules hook that fires before the Void event.
        /// </summary>        
        /// <param name="package">Package object containing the package that was voided.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreVoid(Package package, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that can be used to override the Void event.
        /// </summary>
        /// <param name="package">Package object containing the package that was voided.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public Package VoidPackage(Package package, SerializableDictionary userParams)
        {
            return package;
        }

        ///<summary>
        /// Business rules hook that fires after the Void event.
        /// </summary>
        /// <param name="package">Package object containing the package that was voided.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostVoid(Package package, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Group Rules
        ///<summary>
        /// Business rules hook that fires before the CloseGroup event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="groupType">Grouping Symbol.</param>
        /// <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreCloseGroup(string carrier, string groupType, SerializableDictionary userParams)
        {

        }

        ///<summary>
        /// Business rules hook that fires after the CloseGroup event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="groupType">Grouping Symbol.</param>
        /// <param name="group">Group object.</param>
        /// <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostCloseGroup(string carrier, string groupType, Group group, SerializableDictionary userParams)
        {

        }

        ///<summary>
        /// Business rules hook that fires before the CreateGroup event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="groupType">Grouping Symbol.</param>
        /// <param name="packageRequest">PackageRequest object.</param>
        /// <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        /// 
        public void PreCreateGroup(string carrier, string groupType, PackageRequest packageRequest, SerializableDictionary userParams)
        {

        }

        ///<summary>
        /// Business rules hook that fires after the CreateGroup event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="groupType">Grouping Symbol.</param>
        /// <param name="group">Group object.</param>
        /// <param name="packageRequest">PackageRequest object.</param>
        /// <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        /// 
        public void PostCreateGroup(string carrier, string groupType, Group group, PackageRequest packageRequest, SerializableDictionary userParams)
        {

        }

        /// <summary>
        ///  Business rules hook that fires before the ModifyGroup event.
        ///  </summary>
        ///  <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        ///  <param name="groupId"></param>
        ///  <param name="groupType">Grouping Symbol.</param>
        ///  <param name="packageRequest">PackageRequest object.</param>
        ///  <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        ///  
        public void PreModifyGroup(string carrier, long groupId, string groupType, PackageRequest packageRequest, SerializableDictionary userParams)
        {

        }

        /// <summary>
        ///  Business rules hook that fires after the ModifyGroup event.
        ///  </summary>
        ///  <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        ///  <param name="group">Group</param>
        ///  <param name="groupType">Grouping Symbol.</param>
        ///  <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostModifyGroup(string carrier, Group group, string groupType, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Modify Package List Rules
        ///<summary>
        /// Business rules hook that fires before the ModifyPackageList event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="globalMsns">Global Msns.</param>
        /// <param name="package">Package.</param>
        /// <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreModifyPackageList(string carrier, List<long> globalMsns, Package package, SerializableDictionary userParams)
        {

        }

        ///<summary>
        /// Business rules hook that fires after the ModifyPackageList event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="modifyPackageListResult">Modify Package List Result.</param>
        /// <param name="package">Package.</param>
        /// <param name="userParams">SerializableDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostModifyPackageList(string carrier, ModifyPackageListResult modifyPackageListResult, Package package, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Transmit Rules
        /// <summary>
        /// Business rules hook that fires before the Transmit event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="shipper">ShipExec Shipper Symbol. Example: TEX</param>
        /// <param name="itemsToTransmit">Transmit file symbolic name. Example: DOM_2_20000310_1_1</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreTransmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that can be used to override the Transmit event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="shipper">ShipExec Shipper Symbol. Example: TEX</param>
        /// <param name="itemsToTransmit">Transmit file symbolic name. Example: DOM_2_20000310_1_1</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public List<TransmitItemResult> Transmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook that fires after the Transmit event.
        /// </summary>
        /// <param name="carrier">ShipExec Carrier Symbol. Example: CONNECTSHIP_UPS.UPS</param>
        /// <param name="shipper">ShipExec Shipper Symbol. Example: TEX</param>
        /// <param name="itemsToTransmit">List of transmit file symbolic names that were transmitted. Example: DOM_2_20000310_1_1</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostTransmit(string carrier, string shipper, List<TransmitItem> itemsToTransmit, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Batch
        /// <summary>
        /// Business rules hook that sets the list of batch references displayed on the Batch screen in the shipping client.
        /// </summary>       
        /// <param name="userParams">SerializableDictionary that can be used to pass in key/value pairs for custom use.</param>
        /// <returns>List of BatchReference</returns>
        public List<BatchReference> GetBatchReferences(SerializableDictionary userParams)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook that loads a batch.
        /// </summary>
        /// <param name="batchReference">Batch Reference</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        /// <returns>BatchRequest</returns>
        public BatchRequest LoadBatch(string batchReference, SerializableDictionary userParams)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook to parse a custom file of batch records.
        /// </summary>
        /// <param name="batchReference"></param>
        /// <param name="fileStream">File to be parsed</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        /// <returns></returns>
        public BatchRequest ParseBatchFile(string batchReference, System.IO.Stream fileStream, SerializableDictionary userParams)
        {
            // Example 
            //List<Shipper> shippers = Profile.Shippers;

            //string usersShipper = string.Empty;

            //foreach (var shipper in shippers)
            //{
            //    usersShipper = shipper.Name;
            //    break;
            //}

            //CreateBatchRequest createBatchRequest = new CreateBatchRequest(Logger);
            //BatchRequest batchRequest = createBatchRequest.GetBatchRequest(batchReference, userParams, BusinessRuleSettings, fileStream, ClientContext, usersShipper);

            //return batchRequest;
            return null;
        }

        /// <summary>
        /// Business rules hook that fires before the ProcessBatch event.
        /// </summary>
        /// <param name="batchRequest">ProcessBatchRequest object containing the information for the batch to be processed.</param>
        /// <param name="batchActions">Batch Actions</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreProcessBatch(BatchRequest batchRequest, ProcessBatchActions batchActions, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that fires after the ProcessBatch event.
        /// </summary>
        /// <param name="batchRequest">ProcessBatchRequest object containing the information for the batch that was processed.</param>
        /// <param name="batchActions">Batch Actions</param>
        /// <param name="processBatchResult">ProcessBatchResponse object containing the response from the ProcessBatch operation.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostProcessBatch(BatchRequest batchRequest, ProcessBatchActions batchActions, ProcessBatchResult processBatchResult, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Pack
        /// <summary>
        /// Business rules hook that fires before the PackRate event.
        /// </summary>
        /// <param name="packingRateRequest">PackRateRequest object containing the information for the package to be pack rated.</param>        
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PrePackRate(PackingRateRequest packingRateRequest, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that fires after a PackRate event.
        /// </summary>
        /// <param name="packingRateRequest">PackRateRequest object containing the information for the package that was pack rated.</param>        
        /// <param name="packingRateResponse">PackRateResponse object containing the response from the PackRate operation.</param>        
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostPackRate(PackingRateRequest packingRateRequest, PackingRateResponse packingRateResponse, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that fires before the Pack event.
        /// </summary>
        /// <param name="packingRequest">PackRequest object containing the information for the package to be packed.</param>        
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PrePack(PackingRequest packingRequest, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that fires after a Pack event.
        /// </summary>
        /// <param name="packingRequest">PackRequest object containing the information for the package that was packed.</param>        
        /// <param name="packingResponse">PackResponse object containing the response from the Pack operation.</param>        
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostPack(PackingRequest packingRequest, PackingResponse packingResponse, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Address Validation
        /// <summary>
        /// Business rules hook that fires before the AddressValidation event.
        /// </summary>
        /// <param name="nameAddress">NameAddress object containing the address information to be validated.</param>
        /// <param name="useSimpleNameAddress">Boolean flag to convert the Sox NameAddress object to a Progistics SimpleNameAddress object</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PreAddressValidation(NameAddress nameAddress, bool useSimpleNameAddress, SerializableDictionary userParams)
        {

        }

        /// <summary>
        /// Business rules hook that can be used to override the AddressValidation event.
        /// </summary>
        /// <param name="nameAddress">NameAddress object containing the address information to be validated.</param>
        /// <param name="useSimpleNameAddress">Boolean flag to convert the Sox NameAddress object to a Progistics SimpleNameAddress object</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        /// <returns><![CDATA[ List<NameAddressValidationDandidate> ]]></returns>
        public List<NameAddressValidationCandidate> AddressValidation(NameAddress nameAddress, bool useSimpleNameAddress, SerializableDictionary userParams)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook that fires after the AddressValidation event.
        /// </summary>
        /// <param name="nameAddress">NameAddress object containing the address information that was sent for validation.</param>
        /// <param name="addressValidationCandidates">List of NameAddressValidationCandidate objects containing possible corrected addresses for the address that was validated. If only one candidate is returned then it SHOULD be the correct address.</param>
        /// <param name="userParams">SoxDictionary object that can be used to pass in key/value pairs for custom use.</param>
        public void PostAddressValidation(NameAddress nameAddress, List<NameAddressValidationCandidate> addressValidationCandidates, SerializableDictionary userParams)
        {

        }
        #endregion

        #region Misc

        /// <summary>
        /// Business rules hook that sets the list of box types displayed on the shipping screen in the shipping client.
        /// </summary>
        /// <param name="definedBoxTypes">Box types from profile</param>
        /// <returns>SoxDictionary</returns>
        public List<BoxType> GetBoxTypes(List<BoxType> definedBoxTypes)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook to load a distribution list
        /// </summary>
        /// <param name="value">Distribution list value.</param>
        /// <param name="shipmentRequest">ShipmentRequest object containing the shipment information to be shipped.</param>
        /// <param name="userParams">SoxDictionary object that contins box types that will be displayed in the shipping client.</param>
        /// <returns><![CDATA[ List<ShipmentRequest> ]]></returns>
        public List<ShipmentRequest> LoadDistributionList(string value, ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook that is exposed through the IwcfShip interface for users interacting with ShipExec through the API.
        /// </summary>       
        /// <param name="userObject">An object passed in from the API call.</param>
        /// <returns>object</returns>
        public object UserMethod(object userObject)
        {


            return null;
        }

        /// <summary>
        /// Business rules hook that sets the list of available commodity contents that are available on the commodities screen in the shipping client.
        /// </summary>
        /// <param name="definedCommodityContents">Commodity Contents from profile.</param>
        /// <returns>List of Commodity Content</returns>
        public List<CommodityContent> GetCommodityContents(List<CommodityContent> definedCommodityContents)
        {
            return null;
        }

        /// <summary>
        /// Business rules hook that sets the list of available hazmat contents that are available on the hazmat screen in the shipping client.
        /// </summary>
        /// <param name="definedHazmatContents">Hazmat Contents from profile.</param>
        /// <returns>List of Commodity Content</returns>

        public List<HazmatContent> GetHazmatContents(List<HazmatContent> definedHazmatContents)
        {
            return null;
        }
        #endregion
    }
}
