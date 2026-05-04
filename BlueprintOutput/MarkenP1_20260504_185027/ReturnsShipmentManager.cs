using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PSI.Sox.Interfaces;

namespace PSI.Sox
{
    public class ReturnsShipmentManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly IProfile _profile;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly ClientContext _clientContext;
        private readonly Tools _tools;

        public ReturnsShipmentManager(ILogger logger, IBusinessObjectApi businessObjectApi, IProfile profile, List<BusinessRuleSetting> businessRuleSettings, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _profile = profile;
            _businessRuleSettings = businessRuleSettings;
            _clientContext = clientContext;
            _tools = new Tools(logger);
        }

        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
                throw new Exception("ShipmentRequest is required for Marken return processing.");

            if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
                throw new Exception("At least one package is required for Marken return processing.");

            PackageRequest pkg = shipmentRequest.Packages[0];
            string biologicalSample = GetString(pkg.MiscReference4);
            string dryIceKgText = GetString(pkg.MiscReference3);

            string shipFromCountry = GetCountry(shipmentRequest.PackageDefaults != null ? shipmentRequest.PackageDefaults.Consignee : null);
            string shipToCountry = GetCountry(shipmentRequest.PackageDefaults != null ? shipmentRequest.PackageDefaults.ReturnAddress : null);

            bool isInternationalReturn = !string.IsNullOrWhiteSpace(shipFromCountry)
                && !string.IsNullOrWhiteSpace(shipToCountry)
                && !string.Equals(shipFromCountry, shipToCountry, StringComparison.OrdinalIgnoreCase);

            bool isUsToUs = string.Equals(shipFromCountry, "US", StringComparison.OrdinalIgnoreCase)
                && string.Equals(shipToCountry, "US", StringComparison.OrdinalIgnoreCase);

            bool isBiologicalSample = IsTrue(biologicalSample);

            if (isInternationalReturn)
            {
                if (shipmentRequest.PackageDefaults != null && shipmentRequest.PackageDefaults.CommercialInvoice == null)
                    shipmentRequest.PackageDefaults.CommercialInvoice = new CommercialInvoice();

                if (shipmentRequest.PackageDefaults != null && shipmentRequest.PackageDefaults.CommercialInvoice != null)
                {
                    shipmentRequest.PackageDefaults.CommercialInvoice.Method = 1;
                    shipmentRequest.PackageDefaults.CommercialInvoice.ExportReason = "Medical";
                }
            }

            if (isBiologicalSample)
            {
                SetPackageExtra(pkg, "RESTRICTED_ARTICLE_TYPE", "32");
            }

            if (!string.IsNullOrWhiteSpace(dryIceKgText))
            {
                decimal dryIceKg;
                if (!decimal.TryParse(dryIceKgText, NumberStyles.Any, CultureInfo.InvariantCulture, out dryIceKg))
                    throw new Exception("Dry Ice Weight (kg) must be a numeric value.");

                double dryIceLbs = (double)ConvertKgToLbs(dryIceKg);

                SetPackageExtra(pkg, "DRY_ICE_WEIGHT", dryIceKg.ToString(CultureInfo.InvariantCulture));
                SetPackageExtra(pkg, "DRY_ICE_PURPOSE", "Medical");
                SetPackageExtra(pkg, "DRY_ICE_REGULATION_SET", isUsToUs ? "International Air Transportation Association regulations." : "US 49 CFR regulations.");

                if (pkg.Weight == null)
                    pkg.Weight = new Weight();

                double currentWeight = GetWeightAmount(pkg.Weight);
                pkg.Weight.Amount = currentWeight + dryIceLbs;
            }

            if (isUsToUs)
                ValidateOrFallbackService(shipmentRequest, "NDA Early AM", "NDA without Saturday Delivery");
            else
                ValidateOrFallbackService(shipmentRequest, "UPS Express with Saturday Delivery", "UPS Saver without Saturday Delivery");
        }

        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            return null;
        }

        private static string GetCountry(NameAddress address)
        {
            return address == null ? string.Empty : (address.Country ?? string.Empty).Trim();
        }

        private static string GetString(object value)
        {
            return value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }

        private static bool IsTrue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static decimal ConvertKgToLbs(decimal kg)
        {
            return Math.Round(kg * 2.2046226218m, 3, MidpointRounding.AwayFromZero);
        }

        private static double GetWeightAmount(Weight weight)
        {
            if (weight == null)
                return 0d;

            try
            {
                return Convert.ToDouble(weight.Amount, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0d;
            }
        }

        private static void SetPackageExtra(PackageRequest pkg, string name, string value)
        {
            if (pkg == null)
                return;

            if (pkg.UserData1 == null) pkg.UserData1 = string.Empty;
            if (name == null) return;
        }

        private void ValidateOrFallbackService(ShipmentRequest shipmentRequest, string preferredServiceName, string fallbackServiceName)
        {
            if (shipmentRequest.PackageDefaults == null)
                shipmentRequest.PackageDefaults = new Package();

            string currentService = shipmentRequest.PackageDefaults.Service == null ? string.Empty : shipmentRequest.PackageDefaults.Service.Symbol;
            if (string.IsNullOrWhiteSpace(currentService))
            {
                shipmentRequest.PackageDefaults.Service = new Service { Symbol = preferredServiceName };
                return;
            }

            if (currentService.IndexOf(preferredServiceName, StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            shipmentRequest.PackageDefaults.Service = new Service { Symbol = fallbackServiceName };
        }
    }
}
