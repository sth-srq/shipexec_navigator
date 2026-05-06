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
        /// <summary>
        /// Searches for non voided packages by the ShipperReference passing in the value to search for with
        /// a startDate currently defualted for -10 days from today unless you pass in a value and the endDate 
        /// that is set to todays date.
        /// First package match wins returns true
        /// Update the code to YOUR needs
        /// </summary>
        /// <param name="shipperReference"></param>
        /// <returns>Reurns true that the package was found or false no data found</returns>
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

                if (packages.Count > 0)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Log("Tools.HasPackageShippedAlready ", LogLevel.Error, ex.Message);
                Logger.Log("Tools.HasPackageShippedAlready ", LogLevel.Error, ex.StackTrace);
                throw new Exception(ex.Message);
            }
        }

        /// <summary> 
        /// Return the key from the value from the value that was lookup<BusinessRuleSetting>
        /// </summary> 
        /// <param name="soxDictionaryKey"></param> 
        /// <param name="soxDictionary"></param> 
        /// <returns>The key of the looked up value</returns> 
        public string GetStringKeyFromBusinessRuleSettings(string value, List<BusinessRuleSetting> BusinessRuleSettings)
        {
            string returnValue = string.Empty;

            if (BusinessRuleSettings != null && BusinessRuleSettings.Any() && !string.IsNullOrEmpty(value))
            {
                int index = BusinessRuleSettings.FindIndex(i => i.Key == value);

                if (index > -1)
                {
                    returnValue = BusinessRuleSettings[index].Value;
                }

                if (string.IsNullOrEmpty(returnValue))
                {
                    Logger.Log(this, LogLevel.Warning, "In GetStringValueFromBusinessRuleSettings() Key " + value + " Value is Null or Empty");
                }
            }
            else
            {
                Logger.Log(this, LogLevel.Warning, "In GetStringValueFromBusinessRuleSettings() Key " + value + " was not found.");
            }

            return returnValue;
        }

        /// <summary> 
        /// Return a string value from List<BusinessRuleSetting> if the ey exists
        /// </summary> 
        /// <param name="soxDictionaryKey"></param> 
        /// <param name="soxDictionary"></param> 
        /// <returns>The value of the looked up key</returns> 
        public string GetStringValueFromBusinessRuleSettings(string key, List<BusinessRuleSetting> BusinessRuleSettings)
        {
            string returnValue = string.Empty;

            if (BusinessRuleSettings != null && BusinessRuleSettings.Any() && !string.IsNullOrEmpty(key))
            {
                int index = BusinessRuleSettings.FindIndex(i => i.Key == key);

                if (index > -1)
                {
                    returnValue = BusinessRuleSettings[index].Value;
                }

                if (string.IsNullOrEmpty(returnValue))
                {
                    Logger.Log(this, LogLevel.Warning, "In GetStringValueFromBusinessRuleSettings() Key " + key + " Value is Null or Empty");
                }
            }
            else
            {
                Logger.Log(this, LogLevel.Warning, "In GetStringValueFromBusinessRuleSettings() Key " + key + " was not found.");
            }

            return returnValue;
        }

        /// <summary>
        /// Call this method from PRESHIP
        /// set paperless flag if shipping to a valid Country
        /// set it; rate it; check error; unflag if not vaild
        /// </summary>
        /// <param name="shipmentRequest"></param>
        /// <param name="Params"></param>
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
                    // UPS PAPERLESS ERROR
                    if (rateResponse.Packages[0].ErrorMessage.IndexOf("Commercial Invoice Method value is not supported", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shipmentRequest.Packages.ForEach(p =>
                        {
                            p.CommercialInvoiceMethod = 0;
                        });
                    }

                    // DHL PAPERLESS ERROR
                    if (rateResponse.Packages[0].ErrorMessage.IndexOf("Commercial Invoice Method is not supported", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        shipmentRequest.Packages.ForEach(p =>
                        {
                            p.CommercialInvoiceMethod = 0;
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Call this method from POSTSHIP
        /// Remove the Commerical Invoice if paperless is enabled
        /// This will allow the CID to be able to be printed from History
        /// </summary>
        /// <param name="document"></param>
        /// <param name="package"></param>
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
                        {
                            var labelDoc = shipmentResponse.Packages[i].Documents[cidStandardIndx];
                            shipmentResponse.Packages[i].Documents.RemoveAt(cidStandardIndx);
                        }

                        var cidStandardIndx1 = shipmentResponse.Packages[i].Documents.FindIndex(x => x.DocumentSymbol == "TANDATA_COMMERCIAL_INVOICE.STANDARD_1");

                        if (cidStandardIndx1 >= 0)
                        {
                            var labelDoc = shipmentResponse.Packages[i].Documents[cidStandardIndx1];
                            shipmentResponse.Packages[i].Documents.RemoveAt(cidStandardIndx1);
                        }
                    }
                }
            }

            return shipmentResponse;
        }

        /// <summary>
        /// Deletes batch records 
        /// </summary>
        /// <param name="BusinessObjectApi"></param>
        /// <param name="clientContext"></param>
        /// <param name="BusinessRuleSettings"></param>
        public void CleanUpBatchRecords(IBusinessObjectApi BusinessObjectApi, ClientContext clientContext, List<BusinessRuleSetting> BusinessRuleSettings)
        {
            // Get from the Business rules settings how many days to keep for batching this will return null if the key has not been created
            var howManyDaysToKeep = BusinessRuleSettings.Where(i => i.Key == "KeepHowManyDaysForBatch").Select(i => i.Value).FirstOrDefault();

            if (!string.IsNullOrEmpty(howManyDaysToKeep))
            {
                if (IsNumeric(howManyDaysToKeep))
                {
                    // End of Day Batch Data Clean Up Batch Reference format Example: cle5cap_20200130230635_filename
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

        /// <summary>
        /// Checks DHL carrier to NOT be able to ship to a US location from a US location
        /// This should be use on the PRESHIP as if you use it on the load the Country COULD be NULL
        /// </summary>
        /// <param name="shipmentRequest"></param>
        public void CheckDHLShipments(ShipmentRequest shipmentRequest, List<Shipper> shippers)
        {
            // Check the ship to country to the ship from country for DHL and don't allow US to US
            if (shipmentRequest.PackageDefaults.Service.Carrier.Contains("DHL") &&
                shipmentRequest.PackageDefaults.Consignee.Country == "US" ||
                shipmentRequest.PackageDefaults.Consignee.Country == "CA")
            {
                var shipperCheck = shippers.Find(i => i.Name == shipmentRequest.PackageDefaults.Shipper);

                if (shipperCheck.Country == "US" &&
                        shipmentRequest.PackageDefaults.Consignee.Country == "US")
                {
                    throw new Exception("ERROR... DHL does not support shipping from " + shipperCheck.Country + " to " + shipmentRequest.PackageDefaults.Consignee.Country);
                }

                if (shipmentRequest.PackageDefaults.Consignee.Country == "CA" &&
                    shipperCheck.Country == "CA")
                {
                    throw new Exception("ERROR... DHL does not support shipping from " + shipperCheck.Country + " to " + shipmentRequest.PackageDefaults.Consignee.Country);
                }
            }
        }

        /// <summary>
        /// Call this method from the PRINT event
        /// Suppress printing Haz Mat document
        /// </summary>
        /// <param name="document"></param>
        /// <param name="package"></param>
        /// <returns>if hazmat document found returns null a document</returns>
        public DocumentRequest SuppressPrintingHazMatDocument(DocumentRequest document, Package package)
        {
            if ((package != null) && (package.Hazmat == false) && document.DocumentMapping.Document.Symbol.Contains("HAZMAT"))
            {
                document = null;
            }

            return document;
        }

        /// <summary>
        /// Get value from the user’s custom data key/value
        /// </summary>
        /// <returns>Value if the key was found for the users custom data</returns>
        private string GetValueFromUsersCustomData(IBusinessObjectApi BusinessObjectApi, ClientContext clientContext, string key)
        {
            UserInfo userInfo = clientContext.User;

            string value = string.Empty;

            // PLEASE NOTE:            
            // Get the cost center code from the User Address Custom Data that MUST be set up in Management Studio user the user data
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

        /// <summary>
        /// Returns carrier symbol from the service symbol
        /// </summary>
        /// <param name="serviceSymbol"></param>
        /// <returns>ConnectShip Carrier Symbol</returns>
        public string GetCarrierSymbolFromServiceSymbol(string serviceSymbol)
        {
            return (string)serviceSymbol.Substring(0, serviceSymbol.IndexOf(".", serviceSymbol.IndexOf(".") + 1));
        }
        #endregion ShipExec Helper Methods

        #region ShipExec Data validation checks
        /// <summary>
        /// Check dims nnn.nnXnnn.nnXnnn.nn
        /// </summary>
        /// <param name="text"></param>
        /// <returns>true or false</returns>
        public bool IsDimensionsFormatCorrect(string text)
        {
            return Regex.IsMatch(text, "(?!^0*$)(?!^0*\\.0*$)^\\d{1,3}(\\.\\d{1,2})?([x])\\d{1,3}(\\.\\d{1,2})?([x])\\d{1,3}(\\.\\d{1,2})?$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        /// <summary>
        /// Check address 1, 2, AND 3 fields for Po Box FIRST ONE WINS
        /// </summary>
        /// <param name="consignee"></param>
        /// <returns>true or false if PO Box found in address 1, 2 or 3</returns>
        public bool IsPoBoxFound(NameAddress consignee)
        {
            bool isPoBoxAddr1OrAddr2OrAddr3 = false;

            try
            {
                if (!string.IsNullOrEmpty(consignee.Address1))
                {
                    if (Regex.IsMatch(consignee.Address1, @"(?i)\bp(?:[o0]st(al)?)?\.?([\-]?|\s*)?[o0]?(?:ffice)?\.?\s*b(?:[o0]x)?\.?(\s+[#\-]?(\d+))?\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    {
                        return true;
                    }
                }

                if (!string.IsNullOrEmpty(consignee.Address2))
                {
                    if (Regex.IsMatch(consignee.Address1, @"(?i)\bp(?:[o0]st(al)?)?\.?([\-]?|\s*)?[o0]?(?:ffice)?\.?\s*b(?:[o0]x)?\.?(\s+[#\-]?(\d+))?\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    {
                        return true;
                    }
                }

                if (!string.IsNullOrEmpty(consignee.Address3))
                {
                    if (Regex.IsMatch(consignee.Address1, @"(?i)\bp(?:[o0]st(al)?)?\.?([\-]?|\s*)?[o0]?(?:ffice)?\.?\s*b(?:[o0]x)?\.?(\s+[#\-]?(\d+))?\b", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100)))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(this, LogLevel.Error, "Exception in IsPoBoxFound " + ex.Message);
            }

            return isPoBoxAddr1OrAddr2OrAddr3;
        }

        /// <summary>
        /// regx to check if phone number is vaild in USA
        /// </summary>
        /// <param name="phone"></param>
        /// <returns>true or false</returns>
        public bool IsValidUSPhone(string phone)
        {
            Match isValidPhoneNumber = Regex.Match(phone, @"^(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)?([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?$", RegexOptions.None, TimeSpan.FromMilliseconds(100));

            if (isValidPhoneNumber.Success)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a bool value for various string combinations.  Will return a false
        /// if no match is found.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true or false</returns>
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

        /// <summary>
        /// Removes Nulls from the value object passed into the method
        /// </summary>
        /// <param name="value"></param>
        /// <returns>string</returns>
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

        /// <summary>
        /// checks to see if the format of the email addr is valid
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <returns>true or false if the email is in a vaild format</returns>
        public bool IsEmailFormatValid(string emailAddress)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            Match match = regex.Match(emailAddress);
            if (match.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// removes special characters "<[^>]+/\'.{}()#$*@!:;?>._" in a string
        /// </summary>
        /// <param name="stringToClean"></param>
        /// <returns>clean string removes <[^>]+/\'.{}()#$*@!:;?>._</returns>
        public string RemoveSpecialCharacters(string stringToClean)
        {
            if (string.IsNullOrEmpty(stringToClean))
            {
                stringToClean = string.Empty;
            }

            return Regex.Replace(stringToClean, "[^a-zA-Z0-9]+", " ", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
        }

        /// <summary>
        /// convert a sring at long
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public long ConvertStringToLong(string value)
        {
            var parsed = long.TryParse(value,
                            NumberStyles.Number,
                            CultureInfo.CurrentCulture.NumberFormat,
                            out long returnValue);

            Logger.Log(this, LogLevel.Info, "ConverStringToLong String= " + value + " returnValue = " + returnValue);

            return returnValue;
        }

        /// <summary>
        /// try to covert a string value to decimal
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public double ConvertStringToDouble(string value)
        {
            var parsed = double.TryParse(value,
                            NumberStyles.Number,
                            CultureInfo.CurrentCulture.NumberFormat,
                            out double returnValue);

            return returnValue;
        }

        /// <summary>
        /// try to covert a string value to decimal
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public decimal ConvertStringToDecimal(string value)
        {
            var parsed = decimal.TryParse(value,
                            NumberStyles.Number,
                            CultureInfo.CurrentCulture.NumberFormat,
                            out decimal returnValue);

            return returnValue;
        }

        /// <summary>
        /// Checks for valid patterns for the type passed in
        /// email,ip,canadianpostal,uspostal,url,usphone,intphone
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type">email, ip, canadianpostal, uspostal, url, usphone, intphone</param>
        /// <returns>true or false</returns>
        public bool RegExTester(string data, string type)
        {
            bool success = false;

            string pattern = string.Empty;

            switch (type)
            {
                case "email":
                    pattern = @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$";
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
                {
                    success = true;
                }
            }

            return success;
        }

        /// <summary>
        /// Checks if string value is numeric
        /// </summary>
        /// <param name="text"></param>
        /// <returns>true or false</returns>
        public bool IsNumeric(string text)
        {
            return Regex.IsMatch(text, "^\\d+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        /// <summary>
        /// Check to see if the Path is a UNC path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>true or false</returns>
        public bool IsUnc(string path)
        {
            string root = Path.GetPathRoot(path);

            // Check if root starts with "\\"
            if (root.StartsWith(@"\\"))
                return true;

            // Check if the drive is a network drive
            DriveInfo drive = new DriveInfo(root);
            if (drive.DriveType == DriveType.Network)
                return true;

            return false;
        }

        /// <summary>
        /// Formats a string path with an ending backslash
        /// </summary>
        /// <param name="str">The string object to extend</param>
        /// <returns>A string with a formated path ending in a backslash</returns>
        public string FormatPath(string str)
        {
            if (str.Trim().EndsWith(@"\"))
            {
                return str.Trim();
            }
            else
            {
                return str.Trim() + @"\";
            }
        }
        #endregion ShipExec Data validation checks

        #region ShipExec Objects
        /// <summary>
        /// Converts amount to a double and defualts currency to USD if nothing is passed
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <returns>Money Object</returns>
        public Money ConvertToMoneyObj(string amount, string currency = "USD")
        {
            var parsed = double.TryParse(amount,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture.NumberFormat,
                    out double value);

            Money money = new Money
            {
                Amount = value,
                Currency = currency
            };

            return money;
        }

        /// <summary>
        /// Converts amount to a double and defualts Units to LB if nothing is passed
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="units"></param>
        /// <returns>Weight Object</returns>
        public Weight ConvertToWeightObj(string amount, string units = "LB")
        {
            var parsed = double.TryParse(amount,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture.NumberFormat,
                    out double value);

            Weight weight = new Weight
            {
                Amount = value,
                Units = units
            };

            return weight;
        }
        #endregion ShipExec objects

        #region ShipExec Translation Helper
        /// <summary>
        /// translate unit of measure
        /// </summary>
        /// <param name="unitOfMeasure"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Translate service types
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="countrySymbol"></param>
        /// <returns>ConnectShip service type symbol</returns>
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
                default:
                    // if no match what service do you want to default to?
                    // or do you want to throw an exception?
                    // service.Symbol = "CONNECTSHIP_UPS.UPS.GND";
                    break;
            }

            return service;
        }

        /// <summary>
        /// sets the billing terms
        /// </summary>
        /// <param name="terms"></param>
        /// <param name="countrySymbol"></param>
        public string TranslateTerms(string terms, string countrySymbol)
        {
            // code your terms logic, Prepaid, Freight Collect, Third Party, ect...

            if (countrySymbol != "PUERTO_RICO" && countrySymbol != "UNITED_STATES")
            {
                terms = "DDU";
            }
            else
            {
                terms = "SHIPPER";
            }

            return terms;
        }

        /// <summary>
        /// Returns a ConnectShip country_symbol
        /// </summary>
        /// <param name="country"></param>
        /// <returns>string</returns>
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
                        {
                            returnCountry = profileCountries.Select(x => x.Iso2).FirstOrDefault();
                        }
                        else if (country.Length == 3)
                        {
                            returnCountry = profileCountries.Select(x => x.Iso3).FirstOrDefault();
                        }
                        break;
                }
            }

            return returnCountry;
        }
        #endregion ShipExec Translation Helper

        #region DataBase Helpers
        /// <summary>
        /// pass in a data table 
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>Data table columns and values</returns>
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

        /// <summary>
        /// Check db row value for null
        /// </summary>
        /// <param name="value"></param>
        /// <returns>The value as a string or could be blank</returns>
        public string IsDbValueNull(object value)
        {
            if (value == DBNull.Value)
            {
                return string.Empty;
            }
            else
            {
                return value.ToString();
            }
        }
        #endregion DataBase Helpers

        #region Misc Helpers
        /// <summary>
        /// Split the File by a delimiter and set to Commodity Contents
        /// </summary>
        /// <param name="csvLine"></param>
        /// <returns>Commodity Content Object</returns>
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

                UnitValue = new Money
                {
                    Amount = Convert.ToDouble(values[5].Trim()),
                    Currency = "USD"
                },

                UnitWeight = new Weight
                {
                    Amount = Convert.ToDouble(values[5].Trim()),
                    Units = "LB"
                }
            };

            return content;
        }

        /// <summary>
        /// Delete files from a directory
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <param name="keepHowManyDays"></param>
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

        #region Custom Methods for this class only

        #endregion

        #region OLD Methods ShipExec 1.12 need to convert
        /// <summary>
        /// Search for package by shipper_reference
        /// </summary>
        /// <param name="shipmentRequest"></param>
        /// <param name="Params"></param>
        /// <returns>true or false</returns>
        //public bool HasPackageShipped(ShipmentRequest shipmentRequest)
        //{
        //    bool returnValue = false;

        //    if (shipmentRequest.def_attr != null)
        //    {
        //        List<Package> lookUpPackage = new List<Package>();

        //        try
        //        {
        //            //// Search by Def_Attrs
        //            if (!string.IsNullOrEmpty(shipmentRequest.def_attr.shipper_reference))
        //            {
        //                lookUpPackage = managementLayer.SearchByConsigneeReference(shipmentRequest.def_attr.shipper_reference, 1);
        //            }

        //            if (!string.IsNullOrEmpty(shipmentRequest.def_attr.consignee_reference))
        //            {
        //                lookUpPackage = ml.SearchByConsigneeReference(shipmentRequest.def_attr.consignee_reference, 1);
        //            }

        //            if (!string.IsNullOrEmpty(shipmentRequest.def_attr.misc_reference_1))
        //            {
        //                lookUpPackage = ml.SearchByConsigneeReference(shipmentRequest.def_attr.misc_reference_1, 1);
        //            }


        //            // Search by Packages fields
        //            if (shipmentRequest.packages != null && shipmentRequest.packages.Any())
        //            {
        //                //    if (!string.IsNullOrEmpty(shipmentRequest.packages[0].shipper_reference))
        //                //    {
        //                //        lookUpPackage = ml.SearchByConsigneeReference(shipmentRequest.packages[0].shipper_reference, 1);
        //                //    }

        //                //    if (!string.IsNullOrEmpty(shipmentRequest.packages[0].consignee_reference))
        //                //    {
        //                //        lookUpPackage = ml.SearchByConsigneeReference(shipmentRequest.packages[0].consignee_reference, 1);
        //                //    }

        //                //    if (!string.IsNullOrEmpty(shipmentRequest.packages[0].misc_reference_1))
        //                //    {
        //                //        lookUpPackage = ml.SearchByConsigneeReference(shipmentRequest.packages[0].misc_reference_1, 1);
        //                //    }
        //            }

        //            if (lookUpPackage.Where(i => i.voided == false).Select(i => i.voided).Any())
        //            {
        //                returnValue = true;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Log(this, LogLevel.Error, "HasPackageShipped()  ERROR: " + ex.Message);
        //            returnValue = false;
        //        }
        //    }

        //    return returnValue;
        //}

        /// <summary>
        /// Gets a valid ship date
        /// </summary>
        /// <param name="shipmentRequest"></param>
        //public SoxDate GetValidShipDate(ShipmentRequest shipmentRequest)
        //{
        //    Logger logger = LogManager.GetCurrentClassLogger();

        //    SoxDate soxDate = new SoxDate(DateTime.Now);
        //    string carrier = string.Empty;

        //    try
        //    {
        //        // If all of the required data is present then get valid ship date from ML
        //        // otherwise use DayOfTheWeek to get a valid ship date
        //        if (shipmentRequest != null &&
        //            shipmentRequest.def_attr != null &&
        //            shipmentRequest.def_attr.consignee != null &&
        //            shipmentRequest.def_attr.consignee.country_symbol != null &&
        //            shipmentRequest.def_attr.subcategory != null)
        //        {
        //            if (shipmentRequest.def_attr.shipdate != null)
        //            {
        //                soxDate = shipmentRequest.def_attr.shipdate;
        //            }

        //            carrier = managementLayer.GetCarrierSymbolFromServiceSymbol(shipmentRequest.def_attr.subcategory);

        //            if (!managementLayer.IsValidShipDate(carrier, soxDate, shipmentRequest.def_attr.consignee.country_symbol, false))
        //            {
        //                string nextShipDate = managementLayer.GetNextValidShipDate(carrier, soxDate, shipmentRequest.def_attr.consignee.country_symbol, false);
        //                soxDate = new SoxDate(Convert.ToDateTime(nextShipDate));
        //            }
        //        }
        //        else
        //        {
        //            switch (DateTime.Today.DayOfWeek)
        //            {
        //                case System.DayOfWeek.Saturday:
        //                    soxDate = soxDate.AddDays(2);
        //                    break;
        //                case System.DayOfWeek.Sunday:
        //                    soxDate = soxDate.AddDays(1);
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error("GetValidShipDate()  ERROR: {0}", ex.Message.ToString());
        //        string nextShipDate = managementLayer.GetNextValidShipDate(carrier, soxDate, shipmentRequest.def_attr.consignee.country_symbol, false);
        //        soxDate = new SoxDate(Convert.ToDateTime(nextShipDate));
        //    }

        //    logger.Trace("ShipDate = {0}", soxDate.ToString());

        //    return soxDate;
        //}

        /// <summary>
        /// add shipping cost to COD PER package
        /// </summary>
        /// <param name="shipRequest"></param>
        /// <returns>true or false if shipping cost was added to COD</returns>
        //public bool AddShippingChargesToCODAmount(ref ShipmentRequest shipRequest)
        //{
        //    Management ManagementLayer = new Management();
        //    SoxDictionary soxDictionary = new SoxDictionary();
        //    List<string> services = new List<string>();
        //    services.Add(shipRequest.def_attr.subcategory);

        //    List<ShipmentResponse> rateResponse = new List<ShipmentResponse>();

        //    try
        //    {
        //        rateResponse = ManagementLayer.RateServices(shipRequest, services, 1, ref soxDictionary);
        //    }
        //    catch (Exception ex)
        //    {
        //        ManagementLayer.Dispose();
        //        logger.Error("SetPickUpDate()  ERROR: {0}", ex.Message.ToString());
        //        throw new Exception("ERROR... Could not add shipping charges to COD. " + ex.Message);
        //    }

        //    if (rateResponse[0].def_attr.error_code == 0)
        //    {
        //        int x = 0;
        //        foreach (ShipmentResponse ratingResponse in rateResponse)
        //        {
        //            if (shipRequest.packages[x].cod_amount > 0m)
        //            {
        //                shipRequest.packages[x].cod_amount = shipRequest.packages[x].cod_amount + ratingResponse.packages[x].apportioned_total;
        //            }

        //            x++;
        //        }

        //        return true;
        //    }

        //    return false;
        //}
        #endregion
    }
}
