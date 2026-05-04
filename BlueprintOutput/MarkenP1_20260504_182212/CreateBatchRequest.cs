using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PSI.Sox.Interfaces;

namespace PSI.Sox
{
    public class CreateBatchRequest
    {
        Tools Tools;

        public ILogger Logger { get; set; }

        public CreateBatchRequest(ILogger logger)
        {
            Logger = logger;
            Tools = new Tools(Logger);
        }

        /// <summary>
        /// Creates a FULL batch request with a list of batch items
        /// </summary>
        /// <param name="batchReference"></param>
        /// <param name="Params"></param>
        /// <param name="BusinessRuleSettings"></param>
        /// <returns>Batch Request</returns>
        public BatchRequest GetBatchRequest(string batchReference, SerializableDictionary Params, List<BusinessRuleSetting> BusinessRuleSettings, [Optional] Stream fileStream, ClientContext clientContext, string shipper)
        {
            Logger.Log(this, LogLevel.Info, "In GetBatchRequest...");

            string connectionString = string.Empty;
            DataSet batchData = new DataSet();
            DataService dataService = new DataService(Logger);
            bool batchFile = false;
            
            if (fileStream != null)
            {
                batchFile = true;
                batchData = dataService.ParseBatchFile(fileStream, DataService.BatchFileType.DELIMITED);
                Logger.Log(this, LogLevel.Info, "Batching From a File " + batchReference);
            }
            else
            {
                connectionString = Tools.GetStringValueFromBusinessRuleSettings("customersdb", BusinessRuleSettings);

                // batchData = dataService.GetDataByKeyNumber(connectionString, batchReference, DataService.DataSetName.HEADER);
                Logger.Log(this, LogLevel.Info, "Batching From Database " + batchReference);
            }

            BatchRequest batchRequest = new BatchRequest();

            if (batchData != null)
            {
                string userName = SplitUserFromEmailAddress(clientContext);

                batchRequest.BatchReference = userName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + batchReference;
                batchRequest = GetBatchRequestItems(batchData, batchReference, batchRequest, Params, BusinessRuleSettings, batchFile, shipper);
            }
            else
            {
                batchRequest = null;
                Logger.Log(this, LogLevel.Error, "ERROR in GetBatchRequest the database returned null for " + batchReference);
            }

            return batchRequest;
        }

        /// <summary>
        /// Get a list of batch items
        /// </summary>
        /// <param name="batchData"></param>
        /// <param name="batchReference"></param>
        /// <param name="batchRequest"></param>
        /// <param name="Params"></param>
        /// <param name="connectionStrings"></param>
        /// <returns>Batch request with a list of batch items</returns>
        private BatchRequest GetBatchRequestItems(DataSet batchData, string batchReference, BatchRequest batchRequest, SerializableDictionary Params, List<BusinessRuleSetting> BusinessRuleSettings, bool batchFile, string shipper)
        {
            if (batchRequest.BatchItems == null)
            {
                batchRequest.BatchItems = new List<BatchItem>();
            }

            for (int i = 0; i < batchData.Tables[0].Rows.Count; i++)
            {
                var batchItem = new BatchItem();

                batchItem.BatchItemReference = batchReference + "_" + batchData.Tables[0].Rows[i]["PackageSeq"].ToString();
                batchItem.SequenceNumber = i + 1;

                batchItem.ShipmentRequest = GetShipmentRequest(batchData.Tables[0].Rows[i], batchReference, Params, BusinessRuleSettings, batchFile, shipper);

                batchRequest.BatchItems.Add(batchItem);
            }

            return batchRequest;
        }

        /// <summary>
        /// Create Shipment Request
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <param name="batchReference"></param>
        /// <param name="Params"></param>
        /// <returns>Shipment request batch item</returns>
        public ShipmentRequest GetShipmentRequest(DataRow batchDataRow, string batchReference, SerializableDictionary Params, List<BusinessRuleSetting> BusinessRuleSettings, bool batchFile, string shipper)
        {
            ShipmentRequest shipRequest = new ShipmentRequest();
            Package package = new Package();
            shipRequest.PackageDefaults = new Package();

            SetDefAttr(batchDataRow, shipRequest, Params, batchFile, batchReference, BusinessRuleSettings, shipper);
            shipRequest = SetPackageData(batchDataRow, shipRequest, Params, batchReference, BusinessRuleSettings, batchFile);

            return shipRequest;
        }

        #region Default Attribute
        /// <summary>
        /// Set all Default package Attributes
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <param name="shipRequest"></param>
        private void SetDefAttr(DataRow batchDataRow, ShipmentRequest shipRequest, SerializableDictionary Params, bool batchFile, string batchReference, List<BusinessRuleSetting> BusinessRuleSettings, string shipper)
        {
            shipRequest.PackageDefaults.Shipdate = new Date(DateTime.Now);

            SetShipper(shipRequest, shipper);

            shipRequest.PackageDefaults.Consignee = SetConsigneeData(batchDataRow);

            shipRequest.PackageDefaults.ReturnAddress = SetShipFromData(batchDataRow);

            SetServiceType(batchDataRow, shipRequest, Params, batchFile, batchReference);

            SetBillingTerms(batchDataRow, shipRequest);
        }

        /// <summary>
        /// Set Shipper
        /// </summary>
        /// <param name="shipRequest"></param>
        /// <param name="shipper"></param>
        private void SetShipper(ShipmentRequest shipRequest, string shipper)
        {
            // Since the users are attached to ONE shipper number we will just use that one for batching
            shipRequest.PackageDefaults.Shipper = shipper;   //shipper;

        }
        #endregion

        #region Name Address Methods
        /// <summary>
        /// Set consignee data fields
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <returns>consignee NameAddress Obj</returns>
        private NameAddress SetConsigneeData(DataRow batchDataRow)
        {
            NameAddress consignee = new NameAddress();

            consignee.Company = batchDataRow["ShipToCompany"].ToString();
            consignee.Contact = Tools.RemoveSpecialCharacters(batchDataRow["ShipToContact"].ToString());
            consignee.Address1 = batchDataRow["ShipToAddress1"].ToString();
            consignee.Address2 = Tools.RemoveSpecialCharacters(batchDataRow["ShipToAddress2"].ToString());
            consignee.Address3 = Tools.RemoveSpecialCharacters(batchDataRow["ShipToAddress3"].ToString());
            consignee.City = Tools.RemoveSpecialCharacters(batchDataRow["ShipToCity"].ToString());
            consignee.StateProvince = batchDataRow["ShipToState"].ToString();
            consignee.PostalCode = batchDataRow["ShipToZipcode"].ToString();
            consignee.Country = batchDataRow["ShipToCountry"].ToString();                              
            consignee.Phone = Tools.RemoveSpecialCharacters(batchDataRow["ShipToPhone"].ToString());

            // consignee.Residential = true;

            return consignee;
        }

        /// <summary>
        /// Set ship from data fields
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <returns>ship from NameAddress Obj</returns>
        private NameAddress SetShipFromData(DataRow batchDataRow)
        {
            NameAddress shipFromData = new NameAddress();         

            shipFromData.Country = batchDataRow["ShipFromReturnCountry"].ToString();
            shipFromData.Company = Tools.RemoveSpecialCharacters(batchDataRow["ShipFromReturnCompany"].ToString());
            shipFromData.Contact = Tools.RemoveSpecialCharacters(batchDataRow["ShipFromReturnContact"].ToString());
            shipFromData.Address1 = Tools.RemoveSpecialCharacters(batchDataRow["ShipFromReturnAddress1"].ToString());
            shipFromData.Address2 = Tools.RemoveSpecialCharacters(batchDataRow["ShipFromReturnAddress2"].ToString());
            shipFromData.Address3 = Tools.RemoveSpecialCharacters(batchDataRow["ShipFromReturnAddress3"].ToString());
            shipFromData.City = Tools.RemoveSpecialCharacters(batchDataRow["ShipFromReturnCity"].ToString());
            shipFromData.StateProvince = Tools.RemoveSpecialCharacters(batchDataRow["ShipFromReturnState"].ToString());
            shipFromData.PostalCode = Tools.RemoveSpecialCharacters(batchDataRow["ShipFromReturnZipcode"].ToString());
            shipFromData.Phone = batchDataRow["ShipFromReturnPhone"].ToString();

            return shipFromData;
        }

        /// <summary>
        /// Set third party data fields
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <returns>3rd Party data NameAddress Obj</returns>
        private NameAddress SetThirdPartyData(DataRow batchDataRow)
        {
            NameAddress thirdPartyBillingData = new NameAddress();

            thirdPartyBillingData.Company = batchDataRow["ChangeME"].ToString();
            thirdPartyBillingData.Address1 = batchDataRow["ChangeME"].ToString();
            thirdPartyBillingData.Address2 = batchDataRow["ChangeME"].ToString();
            thirdPartyBillingData.Address3 = batchDataRow["ChangeME"].ToString();
            thirdPartyBillingData.City = batchDataRow["ChangeME"].ToString();
            thirdPartyBillingData.StateProvince = batchDataRow["ChangeME"].ToString();
            thirdPartyBillingData.PostalCode = batchDataRow["ChangeME"].ToString();
            thirdPartyBillingData.Country = batchDataRow["ChangeME"].ToString();
            thirdPartyBillingData.Phone = batchDataRow["ChangeME"].ToString();

            return thirdPartyBillingData;
        }
        #endregion

        #region Package Data Method
        /// <summary>
        /// Set all package data
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <param name="shipRequest"></param>
        /// <param name="Params"></param>
        /// <param name="batchReference"></param>
        /// <returns></returns>
        private ShipmentRequest SetPackageData(DataRow batchDataRow, ShipmentRequest shipRequest, SerializableDictionary Params, string batchReference, List<BusinessRuleSetting> BusinessRuleSettings, bool batchFile)
        {
            PackageRequest packageRequest = new PackageRequest();

            SetDimensions(batchDataRow, packageRequest, Params, batchFile);

            SetEmailNotification(batchDataRow, packageRequest);

            SetPackageWeight(batchDataRow, packageRequest, Params, batchFile, batchReference);

            SetReferenceFields(shipRequest, batchDataRow, Params, batchReference, packageRequest);

            // SetUserData(packageRequest, batchDataRow, Params, batchReference, batchFile);

            SetAccessorials(batchDataRow, packageRequest);

            //**** documnets Only does not require commodities ****
            if (shipRequest.PackageDefaults.Consignee.Country != "US" && packageRequest.DocumentsOnly != true)
            {
                SetCommodityContents(shipRequest, packageRequest, batchDataRow, BusinessRuleSettings);
            }

            if (shipRequest.Packages == null)
            {
                shipRequest.Packages = new List<PackageRequest>();
            }

            shipRequest.Packages.Add(packageRequest);

            return shipRequest;
        }
        #endregion

        #region Set package weight
        /// <summary>
        /// Set package weight
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <param name="packageRequest"></param>
        private void SetPackageWeight(DataRow batchDataRow, PackageRequest packageRequest, SerializableDictionary userParams, bool batchFile, string batchReference)
        {
            packageRequest.Weight = Tools.ConvertToWeightObj(batchDataRow["PackageWeight"].ToString().Trim());

            if (packageRequest.Weight.Amount == 0)
            {
                Logger.Log(this, LogLevel.Error, "Batch did not have a wieght greater than 0 for batch " + batchReference);
                throw new Exception("ERROR... You did not provide a WEIGHT for your batch " + batchReference + ". Please provide a weight and try again.");
            }
        }
        #endregion

        #region Package Accessorials
        /// <summary>
        /// Set all Accessorials
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <param name="packageRequest"></param>
        private void SetAccessorials(DataRow batchDataRow, PackageRequest packageRequest)
        {
            packageRequest.Packaging = batchDataRow["Packaging"].ToString();

            packageRequest.Proof = Tools.ConvertToBool(batchDataRow["ProofRequireSignature"].ToString());
            packageRequest.ProofRequireSignature = Tools.ConvertToBool(batchDataRow["ProofRequireSignature"].ToString());

            packageRequest.SaturdayDelivery = Tools.ConvertToBool(batchDataRow["SaturdayDelivery"].ToString());
            packageRequest.DocumentsOnly = Tools.ConvertToBool(batchDataRow["DocumentsOnly"].ToString());
            packageRequest.Description = batchDataRow["Description"].ToString();
        }
        #endregion

        #region Commodity Contents
        /// <summary>
        /// Set Commodity Contents
        /// </summary>
        /// <param name="shipRequest"></param>
        /// <param name="packageRequest"></param>
        /// <param name="batchData"></param>
        private void SetCommodityContents(ShipmentRequest shipRequest, PackageRequest packageRequest, DataRow batchDataRow, List<BusinessRuleSetting> BusinessRuleSettings)
        {
            // Can ONLY have a MAX of 5 line items in the batch file
            for (int i = 1; i < 6; i++)
            {
                if (!string.IsNullOrEmpty(batchDataRow["Item" + i + "ProductCode"].ToString()))
                {
                    CommodityContent commodities = new CommodityContent();
                    commodities.ProductCode = batchDataRow["Item" + i + "ProductCode"].ToString();
                    commodities.Description = batchDataRow["Item" + i + "Description"].ToString();
                    commodities.OriginCountry = batchDataRow["Item" + i + "OriginCountry"].ToString();
                    commodities.Quantity = Tools.ConvertStringToLong(batchDataRow["Item" + i + "Quantity"].ToString());
                    commodities.UnitValue = Tools.ConvertToMoneyObj(batchDataRow["Item" + i + "UnitValue"].ToString());
                    commodities.UnitWeight = Tools.ConvertToWeightObj(batchDataRow["Item" + i + "UnitWeight"].ToString());
                    commodities.QuantityUnitMeasure = "PCS";

                    if (packageRequest.CommodityContents == null)
                    {
                        packageRequest.CommodityContents = new List<CommodityContent>();
                    }

                    packageRequest.CommodityContents.Add(commodities);
                }
            }

            if (packageRequest.CommodityContents == null)
            {
                Logger.Log(this, LogLevel.Error, "ERROR... In SetCommodityContents no commodity data was found in the batch file for ship to address " + batchDataRow["ShipToAddress1"].ToString());
            }   
        }
        #endregion

        #region SetDimensions
        /// <summary>
        /// Sets length, width and height
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="packageRequest"></param>
        private void SetDimensions(DataRow batchDataRow, PackageRequest packageRequest, SerializableDictionary userParams, bool batchFile)
        {
            Dimensions dimensions = new Dimensions();

            dimensions.Length = Tools.ConvertStringToDouble(batchDataRow["Length"].ToString());
            dimensions.Width = Tools.ConvertStringToDouble(batchDataRow["Width"].ToString());
            dimensions.Height = Tools.ConvertStringToDouble(batchDataRow["Height"].ToString());

            packageRequest.Dimensions = dimensions;
        }
        #endregion

        #region Reference Fields
        /// <summary>
        /// Set all erence fields
        /// </summary>
        /// <param name="shipRequest"></param>
        /// <param name="batchDataRow"></param>
        /// <param name="Params"></param>
        /// <param name="batchReference"></param>
        /// <param name="packageRequest"></param>
        private void SetReferenceFields(ShipmentRequest shipRequest, DataRow batchDataRow, SerializableDictionary Params, string batchReference, PackageRequest packageRequest)
        {
            packageRequest.ShipperReference = batchDataRow["ShipperReference"].ToString();
            packageRequest.ConsigneeReference = batchDataRow["ConsigneeReference"].ToString();
            //packageRequest.MiscReference1 = batchDataRow["MiscReference1"].ToString();
            //packageRequest.MiscReference2 = batchDataRow["MiscReference2"].ToString();
            //packageRequest.MiscReference3 = batchDataRow["MiscReference3"].ToString();
        }
        #endregion

        #region User Data Method
        /// <summary>
        /// Set all user_data_? fields
        /// </summary>
        /// <param name="pack"></param>
        /// <param name="batchDataRow"></param>
        /// <param name="Params"></param>
        /// <param name="batchReference"></param>
        private void SetUserData(PackageRequest pack, DataRow batchDataRow, SerializableDictionary Params, string batchReference, bool batchFile)
        {
            //pack.user_data_1 = new SoxDictionary();
            //pack.user_data_1.Add(new SoxDictionaryItem("batchreference", batchReference));
            //pack.user_data_1.Add(new SoxDictionaryItem("carrcontid", batchDataRow["CARR_CONTID"].ToString()));
            //pack.user_data_1.Add(new SoxDictionaryItem("contstatus", "P"));
            //pack.user_data_1.Add(new SoxDictionaryItem("attr1", DateTime.Now.ToString()));
            //pack.user_data_1.Add(new SoxDictionaryItem("batchrecord", "yes"));
            //pack.user_data_1.Add(new SoxDictionaryItem("filebatch", Convert.ToString(batchFile)));
            //pack.user_data_1.Add(new SoxDictionaryItem("optimizeflag", batchDataRow["optimize_flag"].ToString().Trim()));
            //pack.user_data_1.Add(new SoxDictionaryItem("minisoftprinter", Tools.GetStringValueFromSoxDictionary("minisoftprinter", Params)));

            //string minisoftFlag = Tools.GetStringValueFromSoxDictionary("minisoftflag", Params);
            //pack.user_data_1.Add(new SoxDictionaryItem("minisoftflag", minisoftFlag));

            //pack.user_data_3 = new SoxDictionary();
            //pack.user_data_3.Add(new SoxDictionaryItem("test", "tetsing"));
        }
        #endregion

        #region Set Email Address
        /// <summary>
        /// checks the email format and if valid use it
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <param name="packageRequest"></param>
        private void SetEmailNotification(DataRow batchDataRow, PackageRequest packageRequest)
        {
            if (Tools.IsEmailFormatValid(batchDataRow["EmailAddress"].ToString()))
            {
                packageRequest.ShipNotificationEmail = true;
                packageRequest.ShipNotificationAddressEmail = batchDataRow["EmailAddress"].ToString();              
            }
        }
        #endregion

        #region Service Type
        /// <summary>
        /// Translates and sets the carrier service type
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <param name="shipRequest"></param>
        private void SetServiceType(DataRow batchDataRow, ShipmentRequest shipRequest, SerializableDictionary userParams, bool batchFile, string batchReference)
        {
            if (batchFile)
            {
                string serviceType = string.Empty;  

                if (!string.IsNullOrEmpty(serviceType))
                {
                    // set from the screen
                    shipRequest.PackageDefaults.Service = Tools.TranslateServiceType(serviceType);
                }
                else
                {
                    // must have been in the file
                    Service service = new Service
                    {
                        Symbol = batchDataRow["Service"].ToString()
                    };

                    shipRequest.PackageDefaults.Service = service;
                }
            }
            else
            {
                // Must be coming from a DB
                Service service = new Service
                {
                    Symbol = batchDataRow["Service"].ToString()
                };

                shipRequest.PackageDefaults.Service = service;
            }

            if (string.IsNullOrEmpty(shipRequest.PackageDefaults.Service.Symbol))
            {
                Logger.Log(this, LogLevel.Error, "Batch record did not have a Service Type for batch " + batchReference);
                throw new Exception("ERROR... You did not provide a Carrier Service Type for batch " + batchReference + " please try again.");
            }
        }
        #endregion

        #region Billing Terms
        /// <summary>
        /// sets the billing terms
        /// </summary>
        /// <param name="batchDataRow"></param>
        /// <param name="shipRequest"></param>
        private void SetBillingTerms(DataRow batchDataRow, ShipmentRequest shipRequest)
        {
            shipRequest.PackageDefaults.Terms = "SHIPPER";
        }
        #endregion

        #region Custom Methods for this class only
        /// <summary>
        /// Returns the first part of the email address
        /// </summary>
        /// <param name="clientContext"></param>
        /// <returns>string user name</returns>
        private string SplitUserFromEmailAddress(ClientContext clientContext)
        {
            string userName = string.Empty;
            
            if (clientContext != null)
            {
                if (!string.IsNullOrEmpty(clientContext.User.UserName))
                {
                    if (clientContext.User.UserName.Contains('@'))
                    {
                        return clientContext.User.UserName.Split('@')[0];
                    }
                    else
                    {
                        Logger.Log(this, LogLevel.Warning, "Warning In SplitUserFromEmailAddress ClientContext username did not contain an @ and could not be parsed.");
                    }
                }
                else
                {
                    Logger.Log(this, LogLevel.Error, "Error In SplitUserFromEmailAddress clientContext user name was null or empty. Could not get the user name.");
                }
            }
            else
            {
                Logger.Log(this, LogLevel.Error, "Error In SplitUserFromEmailAddress could not parse email address as the ClientContext was null.");
            }

            return userName;
        }
        #endregion
    }
}
