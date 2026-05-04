using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PSI.Sox.Api;
using PSI.Sox.Client;
using PSI.Sox.Interfaces;
using PSI.Sox.Models;

namespace PSI.Sox
{
    public class ReturnsShipmentManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly IProfile _profile;
        private readonly ClientContext _clientContext;
        private readonly Tools _tools;

        public ReturnsShipmentManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _businessRuleSettings = businessRuleSettings;
            _profile = profile;
            _clientContext = clientContext;
            _tools = new Tools(logger);
        }

        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            try
            {
                if (shipmentRequest == null)
                    throw new Exception("Shipment request is missing.");

                if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
                    throw new Exception("At least one package is required for biological returns shipping.");

                var package = shipmentRequest.Packages[0];
                string consigneeCountry = GetCountry(shipmentRequest, package, true);
                string shipperCountry = GetCountry(shipmentRequest, package, false);
                bool isInternational = !IsSameCountry(consigneeCountry, shipperCountry);
                bool isUsToUs = IsCountry(consigneeCountry, "US") && IsCountry(shipperCountry, "US");

                _logger?.Info($"Marken returns PreShip starting. ConsigneeCountry={consigneeCountry}, ShipperCountry={shipperCountry}, International={isInternational}, USToUS={isUsToUs}.");

                if (isInternational)
                {
                    package.CommercialInvoiceMethod = 1;
                    package.ExportReason = "Medical";
                }

                bool isBiologicalSample = GetBooleanFromMiscReference(package, "MiscReference4", true);
                if (isBiologicalSample)
                {
                    EnsurePackageExtra(package, "RESTRICTED_ARTICLE_TYPE", "32");
                }

                decimal? dryIceKg = GetDecimalFromMiscReference(package, "MiscReference3");
                if (dryIceKg.HasValue)
                {
                    decimal dryIceLbs = KgToLbs(dryIceKg.Value);
                    AddToPackageWeight(package, dryIceLbs);
                    package.DryIcePurpose = "Medical";
                    package.DryIceWeight = dryIceLbs;
                    if (isUsToUs)
                        package.DryIceRegulationSet = "International Air Transportation Association regulations.";
                    else
                        package.DryIceRegulationSet = "US 49 CFR regulations.";
                }

                SelectOrFallbackService(shipmentRequest, package, consigneeCountry, shipperCountry, isBiologicalSample);
                shipmentRequest.Packages[0] = package;

                _logger?.Info("Marken returns PreShip completed successfully.");
            }
            catch (Exception ex)
            {
                _logger?.Error($"Marken returns PreShip failed: {ex}");
                throw;
            }
        }

        private void SelectOrFallbackService(ShipmentRequest shipmentRequest, PackageRequest package, string consigneeCountry, string shipperCountry, bool isBiologicalSample)
        {
            bool isUsToUs = IsCountry(consigneeCountry, "US") && IsCountry(shipperCountry, "US");
            string preferredService = isUsToUs ? "NDA_EARLY_AM" : "UPS_EXPRESS_SATURDAY";
            string fallbackService = isUsToUs ? "NDA_NO_SATURDAY" : "UPS_SAVER_NO_SATURDAY";
            bool preferredValid = IsServiceValidViaRateShop(shipmentRequest, preferredService, isBiologicalSample);

            if (!preferredValid)
            {
                _logger?.Info($"Preferred service '{preferredService}' was not validated. Falling back to '{fallbackService}'.");
                package.Service = fallbackService;
                shipmentRequest.PackageDefaults.Service = fallbackService;
                return;
            }

            _logger?.Info($"Preferred service '{preferredService}' validated. Applying to shipment.");
            package.Service = preferredService;
            shipmentRequest.PackageDefaults.Service = preferredService;
        }

        private bool IsServiceValidViaRateShop(ShipmentRequest shipmentRequest, string serviceSymbol, bool isBiologicalSample)
        {
            try
            {
                if (shipmentRequest == null)
                    return false;

                string currentService = shipmentRequest.PackageDefaults != null ? shipmentRequest.PackageDefaults.Service : null;
                if (!string.IsNullOrWhiteSpace(currentService) && string.Equals(currentService, serviceSymbol, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Rate shop validation failed for service '{serviceSymbol}': {ex}");
                return false;
            }
        }

        private static decimal KgToLbs(decimal kg)
        {
            return kg * 2.2046226218m;
        }

        private void AddToPackageWeight(PackageRequest package, decimal weightToAddLbs)
        {
            if (package == null)
                return;

            if (package.Weight == null)
                package.Weight = new Weight();

            decimal current = 0m;
            if (package.Weight.Amount != null)
            {
                decimal parsed;
                if (decimal.TryParse(package.Weight.Amount.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                    current = parsed;
            }

            package.Weight.Amount = current + weightToAddLbs;
        }

        private string GetCountry(ShipmentRequest shipmentRequest, PackageRequest package, bool isConsignee)
        {
            NameAddress address = null;
            if (shipmentRequest != null && shipmentRequest.PackageDefaults != null)
            {
                address = isConsignee ? shipmentRequest.PackageDefaults.Consignee : shipmentRequest.PackageDefaults.Shipper;
            }

            if (address == null && package != null)
            {
                address = isConsignee ? package.Consignee : package.Shipper;
            }

            return address != null ? (address.Country ?? string.Empty).Trim() : string.Empty;
        }

        private bool IsSameCountry(string country1, string country2)
        {
            return string.Equals(NormalizeCountry(country1), NormalizeCountry(country2), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCountry(string country, string expected)
        {
            return string.Equals(NormalizeCountry(country), NormalizeCountry(expected), StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeCountry(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
                return string.Empty;

            string value = country.Trim().ToUpperInvariant();
            if (value == "UNITED STATES" || value == "USA" || value == "U.S.A.")
                return "US";
            if (value == "CANADA")
                return "CA";
            if (value == "UNITED KINGDOM" || value == "UK")
                return "GB";
            return value;
        }

        private bool GetBooleanFromMiscReference(PackageRequest package, string propertyName, bool defaultValue)
        {
            object raw = GetPropertyValue(package, propertyName);
            if (raw == null)
                return defaultValue;

            bool parsedBool;
            if (bool.TryParse(raw.ToString(), out parsedBool))
                return parsedBool;

            return defaultValue;
        }

        private decimal? GetDecimalFromMiscReference(PackageRequest package, string propertyName)
        {
            object raw = GetPropertyValue(package, propertyName);
            if (raw == null)
                return null;

            decimal parsed;
            if (decimal.TryParse(raw.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                return parsed;

            return null;
        }

        private void EnsurePackageExtra(PackageRequest package, string key, string value)
        {
            if (package == null)
                return;

            if (package.PackageExtras == null)
                package.PackageExtras = new SerializableDictionary();

            package.PackageExtras[key] = value;
        }

        private object GetPropertyValue(object target, string propertyName)
        {
            if (target == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var prop = target.GetType().GetProperty(propertyName);
            if (prop == null)
                return null;

            return prop.GetValue(target, null);
        }
    }
}
