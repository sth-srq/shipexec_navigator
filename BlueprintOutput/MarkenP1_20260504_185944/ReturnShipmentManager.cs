using System;
using System.Collections.Generic;
using System.Linq;
using PSI.Sox.Interfaces;

namespace ShipExec.BusinessRules.Helpers
{
    /// <summary>
    /// Encapsulates Marken Phase 1 specimen return shipping rules.
    /// This class holds the authoritative server-side implementation for the blueprint requirements
    /// that must execute even when the browser UI logic fails.
    /// </summary>
    public class ReturnShipmentManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;

        public ReturnShipmentManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _businessRuleSettings = businessRuleSettings;
            _profile = profile;
            _clientContext = clientContext;
        }

        /// <summary>
        /// Applies the server-side shipping requirements for Marken biological return shipments.
        /// Implements the blueprint's SBR PreShip rules.
        /// </summary>
        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
                throw new Exception("Shipment request is required.");

            if (shipmentRequest.PackageDefaults == null)
                shipmentRequest.PackageDefaults = new Package();

            if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
                throw new Exception("At least one package is required for shipping.");

            var consigneeCountry = GetCountry(shipmentRequest.PackageDefaults.Consignee);
            var shipperCountry = GetCountry(shipmentRequest.PackageDefaults.ReturnAddress);
            bool isInternationalReturn = !string.IsNullOrWhiteSpace(consigneeCountry)
                && !string.IsNullOrWhiteSpace(shipperCountry)
                && !string.Equals(consigneeCountry, shipperCountry, StringComparison.OrdinalIgnoreCase);

            if (isInternationalReturn)
            {
                foreach (var pkg in shipmentRequest.Packages)
                {
                    if (pkg == null)
                        continue;

                    pkg.CommercialInvoiceMethod = 1;
                    pkg.ExportReason = "Medical";
                }
            }

            bool biologicalSample = GetBooleanFromString(GetPackageDefaultString(shipmentRequest.PackageDefaults, "MiscReference4"));
            if (biologicalSample)
            {
                _logger?.Log(this, LogLevel.Info, "Biological Sample flag is true; applying restricted article and dry ice rules.");
            }

            if (biologicalSample)
            {
                foreach (var pkg in shipmentRequest.Packages)
                {
                    if (pkg == null)
                        continue;

                    if (pkg.PackageExtras == null)
                        pkg.PackageExtras = new List<PackageExtra>();

                    EnsurePackageExtra(pkg.PackageExtras, "RESTRICTED_ARTICLE_TYPE", "32");
                }
            }

            string dryIceKgText = GetPackageDefaultString(shipmentRequest.PackageDefaults, "MiscReference3");
            if (!string.IsNullOrWhiteSpace(dryIceKgText))
            {
                decimal dryIceKg;
                if (!decimal.TryParse(dryIceKgText, out dryIceKg))
                    throw new Exception("Dry Ice Weight must be a numeric value in kilograms.");

                decimal dryIceLbs = dryIceKg * 2.2046226218m;

                foreach (var pkg in shipmentRequest.Packages)
                {
                    if (pkg == null)
                        continue;

                    pkg.DryIceWeight = dryIceLbs;
                    pkg.DryIcePurpose = "Medical";

                    bool isUsToUs = string.Equals(shipperCountry, "US", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(consigneeCountry, "US", StringComparison.OrdinalIgnoreCase);

                    pkg.DryIceRegulationSet = isUsToUs
                        ? "International Air Transportation Association regulations."
                        : "US 49 CFR regulations.";

                    if (pkg.Weight == null)
                        pkg.Weight = new Weight();

                    pkg.Weight.Amount = pkg.Weight.Amount + dryIceLbs;
                }
            }

            foreach (var pkg in shipmentRequest.Packages)
            {
                if (pkg == null)
                    continue;

                if (string.Equals(shipperCountry, "US", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(consigneeCountry, "US", StringComparison.OrdinalIgnoreCase))
                {
                    if (!ServiceSelectionManager.IsServiceValidForShipment(shipmentRequest, pkg, "NDA Early AM", _businessObjectApi, _logger))
                    {
                        pkg.Service = "NDA";
                        pkg.SaturdayDelivery = false;
                    }
                    else
                    {
                        pkg.Service = "NDA Early AM";
                    }
                }
                else
                {
                    if (!ServiceSelectionManager.IsServiceValidForShipment(shipmentRequest, pkg, "UPS Express", _businessObjectApi, _logger))
                    {
                        pkg.Service = "UPS Saver";
                        pkg.SaturdayDelivery = false;
                    }
                    else
                    {
                        pkg.Service = "UPS Express";
                        pkg.SaturdayDelivery = true;
                    }
                }
            }
        }

        private static string GetCountry(NameAddress address)
        {
            if (address == null)
                return null;

            return string.IsNullOrWhiteSpace(address.Country) ? null : address.Country.Trim();
        }

        private static bool GetBooleanFromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            bool parsedBool;
            if (bool.TryParse(value, out parsedBool))
                return parsedBool;

            var trimmed = value.Trim();
            return string.Equals(trimmed, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "y", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetPackageDefaultString(Package packageDefaults, string propertyName)
        {
            if (packageDefaults == null)
                return string.Empty;

            var prop = packageDefaults.GetType().GetProperty(propertyName);
            if (prop == null)
                return string.Empty;

            var value = prop.GetValue(packageDefaults, null);
            return value == null ? string.Empty : value.ToString();
        }

        private static void EnsurePackageExtra(List<PackageExtra> packageExtras, string code, string value)
        {
            if (packageExtras == null)
                return;

            var existing = packageExtras.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                packageExtras.Add(new PackageExtra { Code = code, Value = value });
                return;
            }

            existing.Value = value;
        }
    }
}
