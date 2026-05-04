using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Data;
using System.Globalization;
using System.IO;
using PSI.Sox.Interfaces;

namespace PSI.Sox
{
    public class Tools
    {
        public ILogger Logger { get; set; }

        public Tools(ILogger logger)
        {
            Logger = logger;
        }

        #region ShipExec Helper Methods
        public bool HasPackageShippedAlready(string shipperReference, IBusinessObjectApi BusinessObjectApi, ClientContext clientContext, int OptionalHowManyDaysFromToday = -10)
        { 
            try
            {
                var startDate = DateTime.Now.AddDays(OptionalHowManyDaysFromToday).ToString("yyyy-MM-dd");
                var endDate = DateTime.Now.ToString("yyyy-MM-dd");

                var searchCriteria = new Data.SearchCriteria
                {
                    Take = 1,
                    WhereClauses = new List<Data.WhereClause>
                    {
                        new Data.WhereClause { FieldName = "Shipdate", FieldValue = startDate, Operator = Data.SearchOperator.GreaterThanOrEqual },
                        new Data.WhereClause { FieldName = "Shipdate", FieldValue = endDate, Operator = Data.SearchOperator.LessThanOrEqual },
                        new Data.WhereClause { FieldName = "Voided", FieldValue = false, Operator = Data.SearchOperator.Equals },
                        new Data.WhereClause { FieldName = "ShipperReference", FieldValue = shipperReference, Operator = Data.SearchOperator.Equals }
                    },
                    OrderByClauses = new List<Data.OrderByClause>
                    {
                        new Data.OrderByClause { FieldName = "GlobalMsn", Direction = "desc" }
                    }
                };
                
                List<HistoryPackage> packages = BusinessObjectApi.SearchPackageHistory(clientContext.GetUserContext(), searchCriteria, out int totalRecords);

                return packages.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.Log("Tools.HasPackageShippedAlready ", LogLevel.Error, ex.Message);
                Logger.Log("Tools.HasPackageShippedAlready ", LogLevel.Error, ex.StackTrace);
                throw new Exception(ex.Message);
            }
        }

        public string GetStringKeyFromBusinessRuleSettings(string value, List<BusinessRuleSetting> BusinessRuleSettings)
        {
            string returnValue = string.Empty;

            if (BusinessRuleSettings != null && BusinessRuleSettings.Any() && !string.IsNullOrEmpty(value))
            {
                int index = BusinessRuleSettings.FindIndex(i => i.Key == value);
                if (index > -1)
                    returnValue = BusinessRuleSettings[index].Value;

                if (string.IsNullOrEmpty(returnValue))
                    Logger.Log(this, LogLevel.Warning, "In GetStringValueFromBusinessRuleSettings() Key " + value + " Value is Null or Empty");
            }
            else
            {
                Logger.Log(this, LogLevel.Warning, "In GetStringValueFromBusinessRuleSettings() Key " + value + " was not found.");
            }

            return returnValue;
        }

        public string GetStringValueFromBusinessRuleSettings(string key, List<BusinessRuleSetting> BusinessRuleSettings)
        {
            string returnValue = string.Empty;

            if (BusinessRuleSettings != null && BusinessRuleSettings.Any() && !string.IsNullOrEmpty(key))
            {
                int index = BusinessRuleSettings.FindIndex(i => i.Key == key);
                if (index > -1)
                    returnValue = BusinessRuleSettings[index].Value;

                if (string.IsNullOrEmpty(returnValue))
                    Logger.Log(this, LogLevel.Warning, "In GetStringValueFromBusinessRuleSettings() Key " + key + " Value is Null or Empty");
            }
            else
            {
                Logger.Log(this, LogLevel.Warning, "In GetStringValueFromBusinessRuleSettings() Key " + key + " was not found.");
            }

            return returnValue;
        }

        public void CheckAndSetPaperless(ShipmentRequest shipmentRequest, IBusinessObjectApi BusinessObjectApi, ClientContext clientContext, SerializableDictionary userParams)
        {
            if (shipmentRequest.PackageDefaults.Consignee.Country != "US")
            {
                shipmentRequest.Packages.ForEach(p => { p.CommercialInvoiceMethod = 1; });

                List<Service> service = new List<Service>();
                service.Add(shipmentRequest.PackageDefaults.Service);

                ShipmentResponse rateResponse = BusinessObjectApi.Rate(clientContext.GetUserContext(), shipmentRequest, service, 0, userParams).First();

                if (rateResponse.Packages[0].ErrorCode != 0)
                {
                    if (rateResponse.Packages[0].ErrorMessage.IndexOf("Commercial Invoice Method value is not supported", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shipmentRequest.Packages.ForEach(p => { p.CommercialInvoiceMethod = 0; });
                    }

                    if (rateResponse.Packages[0].ErrorMessage.IndexOf("Commercial Invoice Method is not supported", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shipmentRequest.Packages.ForEach(p => { p.CommercialInvoiceMethod = 0; });
                    }
                }
            }
        }

        public ShipmentResponse SuppressPrintingCommericalInvoice(ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse)
        {
            if (shipmentResponse.PackageDefaults.ErrorCode == 0)
            {
                if (shipmentRequest.Packages[0].CommercialInvoiceMethod == 1)
                {
                    for (int i = 0; i < shipmentResponse.Packages.Count; i++)
                    {
                        var cidStandardIndx = shipmentResponse.Packages[i].Documents.FindIndex(x => x.DocumentSymbol == "TANDATA_COMMERCIAL_INVOICE.STANDARD");
                        if (cidStandardIndx >= 0)
                            shipmentResponse.Packages[i].Documents.RemoveAt(cidStandardIndx);

                        var cidStandardIndx1 = shipmentResponse.Packages[i].Documents.FindIndex(x => x.DocumentSymbol == "TANDATA_COMMERCIAL_INVOICE.STANDARD_1");
                        if (cidStandardIndx1 >= 0)
                            shipmentResponse.Packages[i].Documents.RemoveAt(cidStandardIndx1);
                    }
                }
            }

            return shipmentResponse;
        }

        public void CleanUpBatchRecords(IBusinessObjectApi BusinessObjectApi, ClientContext clientContext, List<BusinessRuleSetting> BusinessRuleSettings)
        {
            var howManyDaysToKeep = BusinessRuleSettings.Where(i => i.Key == "KeepHowManyDaysForBatch").Select(i => i.Value).FirstOrDefault();

            if (!string.IsNullOrEmpty(howManyDaysToKeep))
            {
                if (IsNumeric(howManyDaysToKeep))
                {
                    List<Batch> batchesReadyToDelete = BusinessObjectApi.GetBatches(clientContext.GetUserContext(), new Data.SearchCriteria(), out int totalRecords)
                            .Where(x => DateTime.ParseExact(x.BatchReference.Split('_')[1].ToString(), "yyyyMMddHHmmss", null) < DateTime.Now.AddDays(-Convert.ToInt32(howManyDaysToKeep))).ToList();

                    batchesReadyToDelete.ForEach(x => BusinessObjectApi.RemoveBatch(clientContext.GetUserContext(), x.BatchReference));
                }
                else
                {
                    Logger.Log(this, LogLevel.Error, "Could not delete any bacthes. Please check to make sure that the KEY called KeepHowManyDaysForBatch has been set up in Management Studio and the VALUE is numeric.");
                }
            }
            else
            {
                Logger.Log(this, LogLevel.Error, "Could not delete any bacthes. Please check to make sure that the KEY called KeepHowManyDaysForBatch has been set up in Management Studio.");
            }
        }

        public void CheckDHLShipments(ShipmentRequest shipmentRequest, List<Shipper> shippers)
        {
            if ((shipmentRequest.PackageDefaults.Service.Carrier.Contains("DHL") && shipmentRequest.PackageDefaults.Consignee.Country == "US") ||
                shipmentRequest.PackageDefaults.Consignee.Country == "CA")
            {
                var shipperCheck = shippers.Find(i => i.Name == shipmentRequest.PackageDefaults.Shipper);

                if (shipperCheck.Country == "US" && shipmentRequest.PackageDefaults.Consignee.Country == "US")
                    throw new Exception("ERROR... DHL does not support shipping from " + shipperCheck.Country + " to " + shipmentRequest.PackageDefaults.Consignee.Country);

                if (shipmentRequest.PackageDefaults.Consignee.Country == "CA" && shipperCheck.Country == "CA")
                    throw new Exception("ERROR... DHL does not support shipping from " + shipperCheck.Country + " to " + shipmentRequest.PackageDefaults.Consignee.Country);
            }
        }

        public DocumentRequest SuppressPrintingHazMatDocument(DocumentRequest document, Package package)
        {
            if ((package != null) && (package.Hazmat == false) && document.DocumentMapping.Document.Symbol.Contains("HAZMAT"))
            {
                document = null;
            }

            return document;
        }

        private string GetValueFromUsersCustomData(IBusinessObjectApi BusinessObjectApi, ClientContext clientContext, string key)
        {
            UserInfo userInfo = clientContext.User;
            string value = string.Empty;
            ClientProfile usersProfile = BusinessObjectApi.GetClientProfile(clientContext.GetUserContext());

            if (usersProfile.UserInformation.Address != null)
            {
                if (usersProfile.UserInformation.Address.CustomData.Count > 0)
                {
                    int index = usersProfile.UserInformation.Address.CustomData.FindIndex(i => i.Key == key);

                    if (index > -1)
                    {
                        value = usersProfile.UserInformation.Address.CustomData[index].Value;
                    }
                    else
                    {
                        return "ERROR... Could not find Key " + key + " set up for  your user";
                    }

                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                    else
                    {
                        Logger.Log(this, LogLevel.Error, "ERROR... Your Key/Value is NOT set up for your user. " + userInfo.UserName);
                        return "Your Key/Value is NOT set up for your user.";
                    }
                }
                else
                {
                    Logger.Log(this, LogLevel.Error, "Your Key/Value is NOT set up for your user. " + userInfo.UserName);
                }
            }
            else
            {
                Logger.Log(this, LogLevel.Error, "Your Key/Value is NOT set up for your user. " + userInfo.UserName);
            }

            return "Your Key/Value is NOT set up for your user.";
        }

        public string GetCarrierSymbolFromServiceSymbol(string serviceSymbol)
        {
            return (string)serviceSymbol.Substring(0, serviceSymbol.IndexOf(".", serviceSymbol.IndexOf(".") + 1));
        }
        #endregion ShipExec Helper Methods

        #region ShipExec Data validation checks
        public bool IsDimensionsFormatCorrect(string text)
        {
            return Regex.IsMatch(text, "(?!^0*$)(?!^0*\\.0*$)^\\d{1,3}(\\.\\d{1,2})?([x])\\d{1,3}(\\.\\d{1,2})?([x])\\d{1,3}(\\.\\d{1,2})?$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        public bool IsPoBoxFound(NameAddress consignee)
        {
            bool isPoBoxAddr1OrAddr2OrAddr3 = false;

            try
            {
                if (!string.IsNullOrEmpty(consignee.Address1) && Regex.IsMatch(consignee.Address1, @"(?i)\bp(?:[o0]st(al)?)?\.?([\-]?|\s*)?[o0]?(?:ffice)?\.?\s*b(?:[o0]x)?\.?((\s+[#\-]?(\d+))?)\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    return true;

                if (!string.IsNullOrEmpty(consignee.Address2) && Regex.IsMatch(consignee.Address2, @"(?i)\bp(?:[o0]st(al)?)?\.?([\-]?|\s*)?[o0]?(?:ffice)?\.?\s*b(?:[o0]x)?\.?((\s+[#\-]?(\d+))?)\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    return true;

                if (!string.IsNullOrEmpty(consignee.Address3) && Regex.IsMatch(consignee.Address3, @"(?i)\bp(?:[o0]st(al)?)?\.?([\-]?|\s*)?[o0]?(?:ffice)?\.?\s*b(?:[o0]x)?\.?((\s+[#\-]?(\d+))?)\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    return true;
            }
            catch (Exception ex)
            {
                Logger.Log(this, LogLevel.Error, "Exception in IsPoBoxFound " + ex.Message);
            }

            return isPoBoxAddr1OrAddr2OrAddr3;
        }

        public bool IsValidUSPhone(string phone)
        {
            Match isValidPhoneNumber = Regex.Match(phone, @"^(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)?([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return isValidPhoneNumber.Success;
        }

        public bool ConvertToBool(string value)
        {
            bool returnValue;

            if (string.IsNullOrEmpty(value))
            {
                Logger.Log(this, LogLevel.Info, "ConvertToBool() Key Value is NULL setting returnValue to FALSE");
                returnValue = false;
            }
            else
            {
                switch (value.ToUpper())
                {
                    case "NO":
                    case "N":
                    case "-1":
                    case "F":
                    case "FALSE":
                    case "0":
                        returnValue = false;
                        break;
                    case "YES":
                    case "Y":
                    case "T":
                    case "TRUE":
                    case "1":
                        returnValue = true;
                        break;
                    default:
                        returnValue = false;
                        Logger.Log(this, LogLevel.Info, "ConvertToBool() Key Value did not translate {0} setting returnValue to FALSE " + value);
                        break;
                }
            }

            return returnValue;
        }

        public string RemoveNullsReturnEmptyString(object value)
        {
            if (value == null)
            {
                value = string.Empty;
            }
            else if (string.IsNullOrEmpty((string)value))
            {
                value = string.Empty;
            }

            return value.ToString().Trim();
        }

        public bool IsEmailFormatValid(string emailAddress)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            Match match = regex.Match(emailAddress);
            return match.Success;
        }

        public string RemoveSpecialCharacters(string stringToClean)
        {
            if (string.IsNullOrEmpty(stringToClean))
            {
                stringToClean = string.Empty;
            }

            return Regex.Replace(stringToClean, "[^a-zA-Z0-9]+", " ", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
        }

        public long ConvertStringToLong(string value)
        {
            var parsed = long.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out long returnValue);
            Logger.Log(this, LogLevel.Info, "ConverStringToLong String= " + value + " returnValue = " + returnValue);
            return returnValue;
        }

        public double ConvertStringToDouble(string value)
        {
            var parsed = double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out double returnValue);
            return returnValue;
        }

        public decimal ConvertStringToDecimal(string value)
        {
            var parsed = decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out decimal returnValue);
            return returnValue;
        }

        public bool RegExTester(string data, string type)
        {
            bool success = false;
            string pattern = string.Empty;

            switch (type)
            {
                case "email":
                    pattern = @"^(?("" )("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(? (\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
                    break;
                case "ip":
                    pattern = "\\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\b";
                    break;
                case "canadianpostal":
                    pattern = "^[ABCEGHJKLMNPRSTVXY]\\d[ABCEFGHIJKLMNOPQRSTUVYXYZ][ ]?\\d[ABCEFGHIJKLMNOPQRSTUVWXYZ]\\d$";
                    break;
                case "uspostal":
                    pattern = @"^(\d{5}-\d{4}|\d{5}|\d{9})$";
                    break;
                case "url":
                    pattern = @"(https?|HTTPS?)://[a-zA-Z0-9/_\-\$\+\(\)\.\'\,\!\*]*";
                    break;
                case "usphone":
                    pattern = "^(?<Toll>[1])?\\-?\\s?\\(?(?<AreaCode>\\d{3})\\)?\\s*\\-?(?<Ext>\\d{3})\\-?\\s?(?<Number>\\d{4})(?x)";
                    break;
                case "intphone":
                    pattern = @"\+(9[976]\d|8[987530]\d|6[987]\d|5[90]\d|42\d|3[875]\d|2[98654321]\d|9[8543210]|8[6421]|6[6543210]|5[87654321]|4[987654310]|3[9643210]|2[70]|7|1)\d{1,14}$";
                    break;
            }

            if (!string.IsNullOrEmpty(pattern))
            {
                Regex regEx = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
                Match match = regEx.Match(data);
                if (match.Success)
                    success = true;
            }

            return success;
        }

        public bool IsNumeric(string text)
        {
            return Regex.IsMatch(text, "^\\d+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        public bool IsUnc(string path)
        {
            string root = Path.GetPathRoot(path);
            if (root.StartsWith(@"\\"))
                return true;

            DriveInfo drive = new DriveInfo(root);
            if (drive.DriveType == DriveType.Network)
                return true;

            return false;
        }

        public string FormatPath(string str)
        {
            if (str.Trim().EndsWith(@"\"))
                return str.Trim();
            else
                return str.Trim() + @"\";
        }
        #endregion ShipExec Data validation checks

        #region ShipExec Objects
        public Money ConvertToMoneyObj(string amount, string currency = "USD")
        {
            var parsed = double.TryParse(amount, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out double value);
            return new Money { Amount = value, Currency = currency };
        }

        public Weight ConvertToWeightObj(string amount, string units = "LB")
        {
            var parsed = double.TryParse(amount, NumberStyles.Number, CultureInfo.CurrentCulture.NumberFormat, out double value);
            return new Weight { Amount = value, Units = units };
        }
        #endregion ShipExec objects

        #region ShipExec Translation Helper
        public string TranslateUnitOfMeasure(string unitOfMeasure)
        {
            switch (unitOfMeasure.ToUpper())
            {
                case "EA":
                    return "PCS";
                default:
                    throw new Exception("Unable to match the commodity Unit Of Measure.");
            }
        }

        public Service TranslateServiceType(string serviceType)
        {
            Service service = new Service();

            switch (serviceType.ToUpper())
            {
                case "UPS Next Day Air Early A.M.":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.NAM";
                    break;
                case "UPS Next Day Air":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.NDA";
                    break;
                case "UPS Next Day Air Saver":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.NDS";
                    break;
                case "UPS 2nd Day Air A.M.":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.2AM";
                    break;
                case "UPS 2nd Day Air":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.2DA";
                    break;
                case "UPS 3 Day Select":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.3DA";
                    break;
                case "Ground":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.GND";
                    break;
                case "UPS Standard to Canada":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.:STD";
                    break;
                case "UPS Worldwide Express":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.EXP";
                    break;
                case "UPS Worldwide Saver":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.EXPSVR";
                    break;
                case "UPS Worldwide Expedited":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.EPD";
                    break;
                case "UPS Worldwide Express Plus":
                    service.Symbol = "CONNECTSHIP_UPS.UPS.EXPPLS";
                    break;
            }

            return service;
        }

        public string TranslateTerms(string terms, string countrySymbol)
        {
            if (countrySymbol != "PUERTO_RICO" && countrySymbol != "UNITED_STATES")
                terms = "DDU";
            else
                terms = "SHIPPER";

            return terms;
        }

        public string GetCountrySymbol(string country, List<Country> profileCountries)
        {
            string returnCountry = "UNITED_STATES";

            if (!string.IsNullOrEmpty(country))
            {
                switch (country.ToUpper())
                {
                    case "US":
                    case "USA":
                        returnCountry = "UNITED_STATES";
                        break;
                    case "MX":
                    case "MEX":
                        returnCountry = "MEXICO";
                        break;
                    case "CA":
                    case "CAN":
                        returnCountry = "CANADA";
                        break;
                    default:
                        if (country.Length == 2)
                            returnCountry = profileCountries.Select(x => x.Iso2).FirstOrDefault();
                        else if (country.Length == 3)
                            returnCountry = profileCountries.Select(x => x.Iso3).FirstOrDefault();
                        break;
                }
            }

            return returnCountry;
        }
        #endregion ShipExec Translation Helper

        #region DataBase Helpers
        public string GetDataTableColumnsAndValues(DataTable dt)
        {
            string fieldnames = string.Empty;

            foreach (DataRow row in dt.Rows)
            {
                foreach (DataColumn column in dt.Columns)
                {
                    fieldnames = fieldnames + column.ColumnName + ": " + row[column].ToString() + Environment.NewLine;
                }

                fieldnames = fieldnames + Environment.NewLine;
            }

            return fieldnames;
        }

        public string IsDbValueNull(object value)
        {
            if (value == DBNull.Value)
                return string.Empty;
            else
                return value.ToString();
        }
        #endregion DataBase Helpers

        #region Misc Helpers
        public CommodityContent FromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');

            CommodityContent content = new CommodityContent
            {
                ProductCode = values[0].Trim(),
                Description = values[1].Trim(),
                OriginCountry = values[2].Trim(),
                Quantity = Convert.ToInt64(values[3].Trim()),
                QuantityUnitMeasure = values[4].Trim(),
                UnitValue = new Money { Amount = Convert.ToDouble(values[5].Trim()), Currency = "USD" },
                UnitWeight = new Weight { Amount = Convert.ToDouble(values[5].Trim()), Units = "LB" }
            };

            return content;
        }

        public void CleanUpFiles(string localFilePath, int keepHowManyDays)
        {
            Logger.Log(this, LogLevel.Info, "Starting to clean up old files.");

            try
            {
                Directory.GetFiles(localFilePath)
                         .Select(f => new FileInfo(f))
                         .Where(f => f.CreationTime < DateTime.Now.AddDays(-keepHowManyDays))
                         .ToList()
                         .ForEach(f => f.Delete());

                Logger.Log(this, LogLevel.Info, "Clean up completed for directory " + localFilePath + " keeping " + keepHowManyDays + " days.");
            }
            catch (Exception ex)
            {
                Logger.Log(this, LogLevel.Error, "ERROR... In CleanUpFiles for directory " + localFilePath + " Keep how many days " + keepHowManyDays);
                throw new Exception("Error... could not delete the files from directory " + localFilePath + " " + ex.InnerException);
            }
        }
        #endregion Misc Helpers
    }
}
