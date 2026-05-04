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
            int packageIndex = 0;

            if (orderData != null)
            {
                shipmentRequest = SetPackageDefaults(orderData, shipmentRequest, keyNumber);
                shipmentRequest = SetPackageData(orderData, packagesDataSet, commodities, shipmentRequest, userParams, keyNumber, packageIndex);
            }
            else
            {
                shipmentRequest = null;
            }

            return shipmentRequest;
        }

        private ShipmentRequest SetPackageDefaults(DataSet orderData, ShipmentRequest shipmentRequest, string keyNumber)
        {
            shipmentRequest.PackageDefaults.Consignee = SetConsigneeData(orderData);
            shipmentRequest.PackageDefaults.Service = SetServiceType(orderData);
            shipmentRequest.PackageDefaults.Terms = SetBillingTerms(orderData, shipmentRequest);
            return shipmentRequest;
        }

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
            return consignee;
        }

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
            }

            return shipmentRequest;
        }

        private PackageRequest SetPackageAccessorials(DataSet orderData, DataSet packagesDataSet, PackageRequest packageRequest)
        {
            packageRequest.Packaging = "CUSTOM";
            return packageRequest;
        }

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
                }
            }

            return shipmentRequest;
        }

        private PackageRequest SetPackageReferenceFields(DataSet orderData, DataSet packagesDataSet, string keyNumber, PackageRequest packageRequest)
        {
            packageRequest.ShipperReference = keyNumber;
            return packageRequest;
        }

        private ShipmentRequest SetShipmentReferenceFields(ShipmentRequest shipmentRequest, DataSet orderData, DataSet packagesDataSet, string keyNumber)
        {
            return shipmentRequest;
        }

        private ShipmentRequest SetShipmentUserData(ShipmentRequest shipmentRequest, DataSet orderData, string keyNumber)
        {
            shipmentRequest.PackageDefaults.UserData1 = string.Empty;
            shipmentRequest.PackageDefaults.UserData2 = string.Empty;
            shipmentRequest.PackageDefaults.UserData3 = string.Empty;
            shipmentRequest.PackageDefaults.UserData4 = string.Empty;
            shipmentRequest.PackageDefaults.UserData5 = string.Empty;
            return shipmentRequest;
        }

        private PackageRequest SetPackageUserData(PackageRequest packRequest, DataSet orderData, DataSet packagesDataSet,string keyNumber)
        {
            packRequest.UserData1 = string.Empty;
            packRequest.UserData2 = string.Empty;
            packRequest.UserData3 = string.Empty;
            packRequest.UserData4 = string.Empty;
            packRequest.UserData5 = string.Empty;
            return packRequest;
        }

        private PackageRequest SetPackageEmailNotification(DataSet orderData, PackageRequest packageRequest)
        {
            if (Tools.IsEmailFormatValid(orderData.Tables[0].Rows[0]["ship_notification_address_email"].ToString()))
            {
                packageRequest.ShipNotificationEmail = (bool)orderData.Tables[0].Rows[0]["ship_notification_email_flag"];
                packageRequest.ShipNotificationAddressEmail = orderData.Tables[0].Rows[0]["ship_notification_address_email"].ToString();
            }

            return packageRequest;
        }

        private Service SetServiceType(DataSet orderData)
        {
            return Tools.TranslateServiceType(orderData.Tables[0].Rows[0]["subcategory"].ToString());
        }

        private string SetBillingTerms(DataSet orderData, ShipmentRequest shipmentRequest)
        {
            return Tools.TranslateTerms(orderData.Tables[0].Rows[0]["terms"].ToString(), shipmentRequest.PackageDefaults.Consignee.Country);
        }

        private PackageRequest SetPackageDimensions(DataSet orderData, PackageRequest packageRequest)
        {
            Dimensions dimensions = new Dimensions();
            dimensions.Height = 2;
            dimensions.Length = 10;
            dimensions.Width = 8;
            packageRequest.Dimensions = dimensions;
            return packageRequest;
        }
    }
}
