using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PSI.Sox
{
    public class ReturnsShipmentManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly List<BusinessRuleSetting> _settings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;

        public ReturnsShipmentManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> settings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _settings = settings;
            _profile = profile;
            _clientContext = clientContext;
        }

        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            if (shipmentRequest == null) throw new Exception("Shipment request is required.");
            if (shipmentRequest.PackageDefaults == null) throw new Exception("Package defaults are required.");
            if (shipmentRequest.PackageDefaults.Consignee == null) throw new Exception("Consignee/Pickup From is required.");
            if (shipmentRequest.PackageDefaults.Shipper == null) throw new Exception("Shipper is required.");
            if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0) throw new Exception("At least one package is required.");

            var pkg = shipmentRequest.Packages[0];
            var consigneeCountry = NormalizeCountry(shipmentRequest.PackageDefaults.Consignee.Country);
            var shipperCountry = NormalizeCountry(shipmentRequest.PackageDefaults.Shipper.Country);
            bool isUsToUs = IsUs(consigneeCountry) && IsUs(shipperCountry);
            bool isCrossBorderOrInternational = !string.IsNullOrWhiteSpace(consigneeCountry) && !string.IsNullOrWhiteSpace(shipperCountry) && !string.Equals(consigneeCountry, shipperCountry, StringComparison.OrdinalIgnoreCase);
            bool biologicalSample = GetBool(pkg, "MiscReference4") || GetBoolFromUserParams(userParams, "MiscReference4");

            if (isCrossBorderOrInternational)
            {
                SetPackageField(pkg, "CommercialInvoiceMethod", 1);
                SetPackageField(pkg, "ExportReason", "Medical");
            }

            if (biologicalSample)
            {
                EnsurePackageExtras(pkg);
                SetPackageExtra(pkg, "RESTRICTED_ARTICLE_TYPE", "32");
            }

            string dryIceKgText = GetString(pkg, "MiscReference3");
            if (string.IsNullOrWhiteSpace(dryIceKgText))
                dryIceKgText = GetStringFromUserParams(userParams, "MiscReference3");

            if (!string.IsNullOrWhiteSpace(dryIceKgText))
            {
                decimal kg = ParseDecimal(dryIceKgText);
                decimal lbs = ConvertKgToLbs(kg);

                SetPackageField(pkg, "DryIceWeight", lbs);
                SetPackageField(pkg, "DryIcePurpose", "Medical");
                SetPackageField(pkg, "DryIceRegulationSet", isUsToUs ? "International Air Transportation Association regulations." : "US 49 CFR regulations.");

                if (pkg.Weight == null)
                    pkg.Weight = new Weight();

                pkg.Weight.Amount = pkg.Weight.Amount + lbs;
            }

            ValidateAndFallbackService(shipmentRequest, isUsToUs);
        }

        private void ValidateAndFallbackService(ShipmentRequest shipmentRequest, bool isUsToUs)
        {
            string requestedService = shipmentRequest.PackageDefaults.Service ?? string.Empty;

            if (isUsToUs)
            {
                if (!IsValidRateShopService(requestedService, "NDA Early AM"))
                {
                    shipmentRequest.PackageDefaults.Service = "NDA without Saturday Delivery";
                    if (shipmentRequest.PackageDefaults.SaturdayDelivery != null)
                        shipmentRequest.PackageDefaults.SaturdayDelivery = false;
                }
            }
            else
            {
                if (!IsValidRateShopService(requestedService, "UPS Express with Saturday Delivery"))
                {
                    shipmentRequest.PackageDefaults.Service = "UPS Saver without Saturday Delivery";
                    if (shipmentRequest.PackageDefaults.SaturdayDelivery != null)
                        shipmentRequest.PackageDefaults.SaturdayDelivery = false;
                }
            }
        }

        private bool IsValidRateShopService(string requestedService, string candidateService)
        {
            return string.Equals((requestedService ?? string.Empty).Trim(), candidateService, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeCountry(string country)
        {
            return string.IsNullOrWhiteSpace(country) ? string.Empty : country.Trim().ToUpperInvariant();
        }

        private static bool IsUs(string country)
        {
            return country == "US" || country == "USA" || country == "UNITED STATES";
        }

        private static decimal ParseDecimal(string value)
        {
            decimal result;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? result : 0m;
        }

        private static decimal ConvertKgToLbs(decimal kg)
        {
            return Math.Round(kg * 2.2046226218m, 2, MidpointRounding.AwayFromZero);
        }

        private static void EnsurePackageExtras(PackageRequest pkg)
        {
            if (pkg.PackageExtras == null) pkg.PackageExtras = new List<PackageExtra>();
        }

        private static void SetPackageExtra(PackageRequest pkg, string key, string value)
        {
            EnsurePackageExtras(pkg);
            var existing = pkg.PackageExtras.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
                pkg.PackageExtras.Add(new PackageExtra { Key = key, Value = value });
            else
                existing.Value = value;
        }

        private static void SetPackageField(PackageRequest pkg, string fieldName, object value)
        {
            switch (fieldName)
            {
                case "CommercialInvoiceMethod":
                    pkg.CommercialInvoiceMethod = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                    break;
                case "ExportReason":
                    pkg.ExportReason = Convert.ToString(value, CultureInfo.InvariantCulture);
                    break;
                case "DryIceWeight":
                    pkg.DryIceWeight = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    break;
                case "DryIcePurpose":
                    pkg.DryIcePurpose = Convert.ToString(value, CultureInfo.InvariantCulture);
                    break;
                case "DryIceRegulationSet":
                    pkg.DryIceRegulationSet = Convert.ToString(value, CultureInfo.InvariantCulture);
                    break;
            }
        }

        private static bool GetBool(PackageRequest pkg, string key)
        {
            var text = GetString(pkg, key);
            bool result;
            return bool.TryParse(text, out result) && result;
        }

        private static string GetString(PackageRequest pkg, string key)
        {
            if (pkg?.PackageExtras == null) return null;
            var item = pkg.PackageExtras.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
            return item?.Value;
        }

        private static bool GetBoolFromUserParams(SerializableDictionary userParams, string key)
        {
            if (userParams == null || !userParams.ContainsKey(key) || userParams[key] == null) return false;
            bool result;
            return bool.TryParse(Convert.ToString(userParams[key], CultureInfo.InvariantCulture), out result) && result;
        }

        private static string GetStringFromUserParams(SerializableDictionary userParams, string key)
        {
            if (userParams == null || !userParams.ContainsKey(key) || userParams[key] == null) return null;
            return Convert.ToString(userParams[key], CultureInfo.InvariantCulture);
        }
    }

    public class PickupAssociationManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly List<BusinessRuleSetting> _settings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;

        public PickupAssociationManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> settings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _settings = settings;
            _profile = profile;
            _clientContext = clientContext;
        }

        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            if (pickup != null)
                return null;

            if (shipmentRequest == null || shipmentRequest.PackageDefaults == null)
                throw new Exception("Unable to create pickup association because shipment data is incomplete.");

            var createdPickup = new Pickup();
            createdPickup.Shipper = shipmentRequest.PackageDefaults.Shipper;
            createdPickup.Consignee = shipmentRequest.PackageDefaults.Consignee;

            return null;
        }
    }

    public class PackageExtra
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
