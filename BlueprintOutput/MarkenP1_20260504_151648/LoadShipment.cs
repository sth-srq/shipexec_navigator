using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using PSI.Sox.Interfaces;

namespace PSI.Sox
{
    public class LoadShipment
    {
        Tools Tools;

        public ILogger Logger { get; set; }

        public LoadShipment(ILogger logger)
        {
            Logger = logger;
            Tools = new Tools(Logger);
        }

        /// <summary>
        /// Entry point to create a FULL ShipmentRequest
        /// </summary>
        /// <param name="keyNumber"></param>
        /// <param name="Params"></param>
        /// <param name="xmlResponse"></param>
        /// <param name="connectionStrings"></param>
        /// <returns>Shipment Request</returns>
        public ShipmentRequest GetShipmentRequest(string keyNumber,ShipmentRequest shipmentRequest, SerializableDictionary userParams, List<BusinessRuleSetting> BusinessRuleSettings)
        {
            Logger.Log(this, LogLevel.Info, "In GetShipmentRequest.");

            string connectionString = Tools.GetStringValueFromBusinessRuleSettings("customersdb", BusinessRuleSettings);

            DataService dataService = new DataService(Logger);
            
            DataSet orderData = dataService.GetDataByKeyNumber(connectionString, keyNumber, DataService.DataSetName.HEADER);

            if (orderData == null)
            {        
                Logger.Log(this, LogLevel.Info, "No record(s) found for '" + keyNumber + "' DataSet[" + DataService.DataSetName.HEADER + "]");
                throw new Exception("No record(s) found for '" + keyNumber + "' DataSet[" + DataService.DataSetName.HEADER + "]");
            }

            DataSet commodities = dataService.GetDataByKeyNumber(connectionString, keyNumber, DataService.DataSetName.COMMODITY);

            if(commodities == null)
            {
                Logger.Log(this, LogLevel.Info, "No record(s) found for '" + keyNumber + "' DataSet[" + DataService.DataSetName.COMMODITY + "]");
                throw new Exception("No record(s) found for '" + keyNumber + "' DataSet[" + DataService.DataSetName.COMMODITY + "]");
            }

            DataSet packagesDataSet = new DataSet();

            //// You can use this for customers that load mulitple packages in the load event.
            //packagesDataSet = dataService.GetDataByKeyNumber(connectionString, keyNumber, DataService.DataSetName.PACKAGES);

            //if(packagesDataSet == null)
            //{
            //    Logger.Log(this, LogLevel.Info,"No record(s) found for '" + keyNumber + "' DataSet[" +  DataService.DataSetName.PACKAGES + "]");
            //    throw new Exception("No record(s) found for '" + keyNumber + "' DataSet[" + DataService.DataSetName.PACKAGES + "]");
            //}

            // packageIndex can be used for customers that load multiple packages on the load event.
            int packageIndex = 0;

            if (orderData != null)
            {
                shipmentRequest = SetPackageDefaults(orderData, shipmentRequest, keyNumber);

                // This method can be placed in a loop if you need to populate multipiece shipments.  Don't forget
                // to do a shipmentRequest.pacakges.Add(packageRequest); if you are doing multipiece shipments
                shipmentRequest = SetPackageData(orderData, packagesDataSet, commodities, shipmentRequest, userParams, keyNumber, packageIndex);
            }
            else
            {
                shipmentRequest = null;
            }

            return shipmentRequest;
        }

        #region Default Attribute
        /// <summary>
        /// Set all Default package Attributes
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="shipmentRequest"></param>
        private ShipmentRequest SetPackageDefaults(DataSet orderData, ShipmentRequest shipmentRequest, string keyNumber)
        {
            // --------------------------------------------------------------------------
            // only set the ShipDate IF there is no ShipDate
            // otherwise you will override the ShipExec
            // thin client ShipDate on the user interface
            //if (shipmentRequest.PackageDefaults.Shipdate == null)
            //{
            //    shipmentRequest.PackageDefaults.Shipdate = new Date(DateTime.Now);
            //}

            shipmentRequest.PackageDefaults.Consignee = SetConsigneeData(orderData);

            // shipmentRequest.PackageDefaults.ReturnAddress = SetShipFromData(orderData);

            // shipmentRequest.PackageDefaults.ImporterOfRecord = SetImporterOfRecord(orderData);

            shipmentRequest.PackageDefaults.Service = SetServiceType(orderData);

            shipmentRequest.PackageDefaults.Terms = SetBillingTerms(orderData, shipmentRequest);

            // shipmentRequest = SetShipmentUserData(shipmentRequest, orderData, keyNumber);

            return shipmentRequest;
        }
        #endregion

        #region Name Address Methods
        /// <summary>
        /// Set consignee data fields
        /// </summary>
        /// <param name="orderData"></param>
        /// <returns>consignee NameAddress Obj</returns>
        private NameAddress SetConsigneeData(DataSet orderData)
        {
            NameAddress consignee = new NameAddress();

            consignee.Country = orderData.Tables[0].Rows[0]["consignee_country_symbol"].ToString();
            consignee.Company = orderData.Tables[0].Rows[0]["consignee_company"].ToString();
            consignee.Contact = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["consignee_contact"].ToString());
            consignee.Address1 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["consignee_address1"].ToString());
            consignee.Address2 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["consignee_address2"].ToString());
            consignee.Address3 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["consignee_address3"].ToString());
            consignee.City = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["consignee_city"].ToString());
            consignee.StateProvince = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["consignee_state_province"].ToString());
            consignee.PostalCode = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["consignee_postal_code"].ToString());
            consignee.Phone = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["consignee_phone"].ToString());
            // consignee.Residential = Tools.ConvertToBool(orderData.Tables[0].Rows[0]["consignee_residential"]);

            //if(Tools.IsPoBoxFound(orderData.Tables[0].Rows[0]["consignee_po_box_flag"]))
            //{
            //    consignee.PoBox = Tools.ConvertToBool(orderData.Tables[0].Rows[0]["consignee_po_box_flag"]);
            //}

            return consignee;
        }

        /// <summary>
        /// Set ship from data fields
        /// </summary>
        /// <param name="orderData"></param>
        /// <returns>ship from NameAddress Obj</returns>
        private NameAddress SetShipFromData(DataSet orderData)
        {
            NameAddress shipFromData = new NameAddress();

            shipFromData.Country = orderData.Tables[0].Rows[0]["return_address_country_symbol"].ToString();
            shipFromData.Company = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["return_address_company"].ToString());
            shipFromData.Contact = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["return_address_contact"].ToString());
            shipFromData.Address1 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["return_address_address1"].ToString());
            shipFromData.Address2 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["return_address_address2"].ToString());
            shipFromData.Address3 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["return_address_address3"].ToString());
            shipFromData.City = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["return_address_city"].ToString());
            shipFromData.StateProvince = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["return_address_state_province"].ToString());
            shipFromData.PostalCode = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["return_address_postal_code"].ToString());
            shipFromData.Phone = orderData.Tables[0].Rows[0]["return_address_phone"].ToString();
   
            return shipFromData;
        }

        /// <summary>
        /// Set third party data fields
        /// </summary>
        /// <param name="orderData"></param>
        /// <returns>3rd Party data NameAddress Obj</returns>
        private ShipmentRequest SetThirdPartyData(DataSet orderData, ShipmentRequest shipmentRequest)
        {
            NameAddress thirdPartyBillingData = new NameAddress();

            bool thirdPartyFlag = (bool)orderData.Tables[0].Rows[0]["third_party_billing"];

            if (thirdPartyFlag)
            {
                shipmentRequest.PackageDefaults.ThirdPartyBilling = true;

                thirdPartyBillingData.Account = orderData.Tables[0].Rows[0]["third_party_account"].ToString();
                thirdPartyBillingData.Country = orderData.Tables[0].Rows[0]["third_party_address_country_symbol"].ToString();
                thirdPartyBillingData.Company = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["third_party_address_company"].ToString());
                thirdPartyBillingData.Address1 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["third_party_address_address1"].ToString());
                thirdPartyBillingData.Address2 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["third_party_address_address2"].ToString());
                thirdPartyBillingData.Address3 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["third_party_address_address3"].ToString());
                thirdPartyBillingData.City = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["third_party_address_city"].ToString());
                thirdPartyBillingData.StateProvince = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["third_party_address_state_province"].ToString());
                thirdPartyBillingData.PostalCode = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["third_party_address_postal_code"].ToString());
                thirdPartyBillingData.Phone = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["third_party_address_phone"].ToString());
                
                shipmentRequest.PackageDefaults.ThirdPartyBillingAddress = thirdPartyBillingData;
            }
            
            return shipmentRequest;
        }

        /// <summary>
        ///  Set importer of record data feilds
        /// </summary>
        /// <param name="orderData"></param>
        /// <returns>importer of record NameAddress Obj</returns>
        private NameAddress SetImporterOfRecord(DataSet orderData)
        {
            NameAddress importOfRecord = new NameAddress();

            importOfRecord.Country = orderData.Tables[0].Rows[0]["importer_of_record_country_symbol"].ToString();
            importOfRecord.Company = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_company"].ToString());
            importOfRecord.Contact = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_contact"].ToString());
            importOfRecord.Address1 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_address1"].ToString());
            importOfRecord.Address2 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_address2"].ToString());
            importOfRecord.Address3 = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_address3"].ToString());
            importOfRecord.City = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_city"].ToString());
            importOfRecord.StateProvince = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_state_province"].ToString());
            importOfRecord.PostalCode = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_postal_code"].ToString());
            importOfRecord.Phone = Tools.RemoveSpecialCharacters(orderData.Tables[0].Rows[0]["importer_of_record_phone"].ToString());
            importOfRecord.Account = orderData.Tables[0].Rows[0]["ImporterOfRecordAccount"].ToString();

            return importOfRecord;
        }
        #endregion

        #region Package Data Method
        /// <summary>
        /// Set all package data
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="shipmentRequest"></param>
        /// <param name="Params"></param>
        /// <param name="keyNumber"></param>
        /// <returns></returns>
        private ShipmentRequest SetPackageData(DataSet orderData, DataSet packagesDataSet,DataSet commodities, ShipmentRequest shipmentRequest, SerializableDictionary Params, string keyNumber, int packageIndex)
        {
            shipmentRequest.Packages[packageIndex] = SetPackageReferenceFields(orderData, packagesDataSet, keyNumber, shipmentRequest.Packages[packageIndex]);

            shipmentRequest.Packages[packageIndex] = SetPackageEmailNotification(orderData, shipmentRequest.Packages[packageIndex]);

            shipmentRequest.Packages[packageIndex] = SetPackageAccessorials(orderData, packagesDataSet, shipmentRequest.Packages[packageIndex]);

            shipmentRequest.Packages[packageIndex] = SetPackageUserData(shipmentRequest.Packages[packageIndex], orderData, packagesDataSet, keyNumber);

            shipmentRequest.Packages[packageIndex] = SetPackageDimensions(orderData, shipmentRequest.Packages[packageIndex]);

            if(shipmentRequest.PackageDefaults.Consignee.Country != "US")
            {
                shipmentRequest = SetCommodityContents(shipmentRequest, orderData, packagesDataSet, commodities, packageIndex);

                //// unrem as needed
                // shipmentRequest.Packages[packageIndex].ImporterOfRecord = SetImporterOfRecord(orderData);
            }

            return shipmentRequest;
        }
        #endregion

        #region Package Accessorials
        /// <summary>
        /// Set all Accessorials
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="packageRequest"></param>
        private PackageRequest SetPackageAccessorials(DataSet orderData, DataSet packagesDataSet, PackageRequest packageRequest)
        {
            //// unrem or add as needed to populate package accessorials
            packageRequest.Packaging = "CUSTOM";
            //packageRequest.Proof = Tools.ConvertToBool(orderData.Tables[0].Rows[0]["Proof"].ToString());
            //packageRequest.SaturdayDelivery = Tools.ConvertToBool(orderData.Tables[0].Rows[0]["SaturdayDelivery"].ToString());
            //packageRequest.DeclaredValueAmount = Tools.ConvertToMoney(orderData.Tables[0].Rows[0]["DeclaredValueAmount"].ToString());

            return packageRequest;
        }
        #endregion

        #region Commodity Contents
        /// <summary>
        /// Set Commodity Contents
        /// </summary>
        /// <param name="shipmentRequest"></param>
        /// <param name="packageRequest"></param>
        /// <param name="orderData"></param>
        private ShipmentRequest SetCommodityContents(ShipmentRequest shipmentRequest, DataSet orderData,DataSet packagesDataSet, DataSet commodities, int packageIndex)
        {
            if (commodities.Tables.Count > 0)
            {
                if (commodities.Tables[0].Rows.Count > 0)
                {

                    for (int i = 0; i < commodities.Tables[0].Rows.Count; i++)
                    {
                        CommodityContent commodity = new CommodityContent();
                        commodity.OriginCountry = commodities.Tables[0].Rows[i]["commodity_origin_country"].ToString();

                        commodity.Description = commodities.Tables[0].Rows[i]["commodity_description"].ToString();
                        commodity.ProductCode = commodities.Tables[0].Rows[i]["commodity_product_code"].ToString();

                        // change unit of measure as needed, the default is PCS for pieces
                        commodity.QuantityUnitMeasure = "PCS";

                        commodity.UnitWeight = Tools.ConvertToWeightObj(commodities.Tables[0].Rows[i]["commodity_unit_weight"].ToString());

                        commodity.UnitValue = Tools.ConvertToMoneyObj(commodities.Tables[0].Rows[i]["commodity_unit_value"].ToString());

                        commodity.Quantity = Tools.ConvertStringToLong(commodities.Tables[0].Rows[i]["commodity_quantity"].ToString());

                        if (shipmentRequest.Packages[packageIndex].CommodityContents == null)
                        {
                            shipmentRequest.Packages[packageIndex].CommodityContents = new List<CommodityContent>();
                        }

                        shipmentRequest.Packages[packageIndex].CommodityContents.Add(commodity);
                    }

                    //shipmentRequest.Packages[packageIndex].Description = orderData.Tables[0].Rows[0]["package_description"].ToString();
                    //shipmentRequest.Packages[packageIndex].ExportReason = "Sale";

                    //// unrem as needed
                    //if (shipmentRequest.PackageDefaults.Consignee.Country == "CANADA")
                    //{
                    //    shipmentRequest.PackageDefaults.ExportDeclarationStatement = "I hereby certify that the goods covered by this shipment qualify as originating goods for " +
                    //                                            " purposes of preferential tariff treatment under the NAFTA.";
                    //}
                }
            }

            return shipmentRequest;
        }
        #endregion

        #region Reference Fields
        /// <summary>
        /// Set package reference fields
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="keyNumber"></param>
        /// <param name="packageRequest"></param>
        private PackageRequest SetPackageReferenceFields(DataSet orderData, DataSet packagesDataSet, string keyNumber, PackageRequest packageRequest)
        {
            packageRequest.ShipperReference = keyNumber;
            //packageRequest.ShipperReference = orderData.Tables[0].Rows[0]["package_shipper_reference"].ToString();
            //packageRequest.ConsigneeReference = orderData.Tables[0].Rows[0]["package_consignee_reference"].ToString();

            //  packageRequest.MiscReference1 = orderData.Tables[0].Rows[0]["package_misc_reference_1"].ToString();
            //  packageRequest.MiscReference2 = orderData.Tables[0].Rows[0]["package_misc_reference_3"].ToString();
            //  packageRequest.MiscReference3 = orderData.Tables[0].Rows[0]["package_misc_reference_3"].ToString();
            //  packageRequest.MiscReference4 = orderData.Tables[0].Rows[0]["package_misc_reference_4"].ToString();
            //  packageRequest.MiscReference5 = orderData.Tables[0].Rows[0]["package_misc_reference_5"].ToString();

            return packageRequest;
        }

        /// <summary>
        /// Set Shipment Defaults reference fields
        /// </summary>
        /// <param name="shipmentRequest"></param>
        /// <param name="orderData"></param>
        /// <param name="packagesDataSet"></param>
        /// <param name="keyNumber"></param>
        /// <returns></returns>
        private ShipmentRequest SetShipmentReferenceFields(ShipmentRequest shipmentRequest, DataSet orderData, DataSet packagesDataSet, string keyNumber)
        {
            //shipmentRequest.PackageDefaults.ShipperReference = string.Empty;
            //shipmentRequest.PackageDefaults.ShipperReference = string.Empty;
            //shipmentRequest.PackageDefaults.ConsigneeReference = string.Empty;

            //shipmentRequest.PackageDefaults.MiscReference1 = string.Empty;
            //shipmentRequest.PackageDefaults.MiscReference2 = string.Empty;
            //shipmentRequest.PackageDefaults.MiscReference3 = string.Empty;
            //shipmentRequest.PackageDefaults.MiscReference4 = string.Empty;
            //shipmentRequest.PackageDefaults.MiscReference5 = string.Empty;

            return shipmentRequest;
        }
        #endregion

        #region User Data Method
        /// <summary>
        /// Set Shipment user_data Objects
        /// </summary>
        /// <param name="shipRequest"></param>
        /// <param name="orderData"></param>
        /// <param name="keyNumber"></param>
        /// <returns>ShipmentRequest</returns>
        private ShipmentRequest SetShipmentUserData(ShipmentRequest shipmentRequest, DataSet orderData, string keyNumber)
        {
            shipmentRequest.PackageDefaults.UserData1 = string.Empty;
            shipmentRequest.PackageDefaults.UserData2 = string.Empty;
            shipmentRequest.PackageDefaults.UserData3 = string.Empty;
            shipmentRequest.PackageDefaults.UserData4 = string.Empty;
            shipmentRequest.PackageDefaults.UserData5 = string.Empty;

            return shipmentRequest;
        }

        /// <summary>
        /// Set Package user_data objects
        /// </summary>
        /// <param name="packRequest"></param>
        /// <param name="orderData"></param>
        /// <param name="keyNumber"></param>
        /// <returns>PackageRequest</returns>
        private PackageRequest SetPackageUserData(PackageRequest packRequest, DataSet orderData, DataSet packagesDataSet,string keyNumber)
        {
            packRequest.UserData1 = string.Empty;
            packRequest.UserData2 = string.Empty;
            packRequest.UserData3 = string.Empty;
            packRequest.UserData4 = string.Empty;
            packRequest.UserData5 = string.Empty;

            return packRequest;
        }
        #endregion

        #region Package Email Address
        /// <summary>
        /// checks the email format and if valid use it
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="packageRequest"></param>
        private PackageRequest SetPackageEmailNotification(DataSet orderData, PackageRequest packageRequest)
        {
            if (Tools.IsEmailFormatValid(orderData.Tables[0].Rows[0]["ship_notification_address_email"].ToString()))
            {
                packageRequest.ShipNotificationEmail = (bool)orderData.Tables[0].Rows[0]["ship_notification_email_flag"];
                packageRequest.ShipNotificationAddressEmail = orderData.Tables[0].Rows[0]["ship_notification_address_email"].ToString();
            }

            return packageRequest;
        }
        #endregion

        #region Service Type
        /// <summary>
        /// Translates and sets the carrier service type
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="shipRequest"></param>
        private Service SetServiceType(DataSet orderData)
        {
            return Tools.TranslateServiceType(orderData.Tables[0].Rows[0]["subcategory"].ToString());
        }
        #endregion

        #region Billing Terms
        /// <summary>
        /// sets the billing terms
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="shipmentRequest"></param>
        private string SetBillingTerms(DataSet orderData, ShipmentRequest shipmentRequest)
        {
            return Tools.TranslateTerms(orderData.Tables[0].Rows[0]["terms"].ToString(), shipmentRequest.PackageDefaults.Consignee.Country);
        }
        #endregion

        #region Package Dimensions
        /// <summary>
        /// Set package dimensions
        /// </summary>
        /// <param name="orderData"></param>
        /// <param name="packageRequest"></param>
        /// <returns>Dimensions Object</returns>
        private PackageRequest SetPackageDimensions(DataSet orderData, PackageRequest packageRequest)
        {
            Dimensions dimensions = new Dimensions();

            dimensions.Height = 2;
            dimensions.Length = 10;
            dimensions.Width = 8;

            packageRequest.Dimensions = dimensions;

            return packageRequest;
        }
        #endregion

        #region Custom Methods for this class only

        #endregion
    }
}
