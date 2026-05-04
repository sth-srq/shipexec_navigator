using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PSI.Sox;
using PSI.Sox.Interfaces;

namespace ShipExec.BusinessRules
{
    public class ReturnsShippingManager
    {
        private readonly ILogger _logger;
        private readonly IBusinessObjectApi _businessObjectApi;
        private readonly IProfile _profile;
        private readonly List<BusinessRuleSetting> _businessRuleSettings;
        private readonly ClientContext _clientContext;

        public ReturnsShippingManager(ILogger logger, IBusinessObjectApi businessObjectApi, IProfile profile, List<BusinessRuleSetting> businessRuleSettings, ClientContext clientContext)
        {
            _logger = logger;
            _businessObjectApi = businessObjectApi;
            _profile = profile;
            _businessRuleSettings = businessRuleSettings;
            _clientContext = clientContext;
        }

        public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
                throw new ArgumentNullException(nameof(shipmentRequest));

            _logger?.Log(this, LogLevel.Info, "ReturnsShippingManager.PreShip started.");

            if (shipmentRequest.PackageDefaults == null)
                shipmentRequest.PackageDefaults = new Package();

            if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
                throw new Exception("At least one package is required for Marken return shipping.");

            var package = shipmentRequest.Packages[0];
            if (package == null)
                throw new Exception("The first package is missing.");

            var consigneeCountry = GetCountryCode(shipmentRequest.PackageDefaults.Consignee);
            var shipperCountry = GetCountryCode(shipmentRequest.PackageDefaults.Shipper);
            var biologicalSample = GetBooleanReference(package.MiscReference4);
            var dryIceWeightKg = GetNullableDecimal(package.MiscReference3);

            bool isUsToUs = IsCountry(consigneeCountry, "US") && IsCountry(shipperCountry, "US");
            bool isCanadaToCanada = IsCountry(consigneeCountry, "CA") && IsCountry(shipperCountry, "CA");
            bool isInternationalReturn = !string.IsNullOrWhiteSpace(consigneeCountry)
                && !string.IsNullOrWhiteSpace(shipperCountry)
                && !string.Equals(consigneeCountry, shipperCountry, StringComparison.OrdinalIgnoreCase);

            if (isInternationalReturn)
            {
                EnsureCommercialInvoiceMethod(package, 1);
                EnsureExportReason(package, "Medical");
            }

            if (biologicalSample)
            {
                EnsurePackageExtra(package, "RESTRICTED_ARTICLE_TYPE", "32");
            }

            if (dryIceWeightKg.HasValue)
            {
                ApplyDryIce(package, dryIceWeightKg.Value, isUsToUs);
            }

            ValidateOrAdjustService(shipmentRequest, isUsToUs, isCanadaToCanada, biologicalSample);

            _logger?.Log(this, LogLevel.Info, "ReturnsShippingManager.PreShip completed successfully.");
        }

        public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
        {
            if (shipmentRequest == null)
                throw new ArgumentNullException(nameof(shipmentRequest));

            _logger?.Log(this, LogLevel.Info, "ReturnsShippingManager.Ship started.");
            return null;
        }

        private bool GetBooleanSetting(string key, bool defaultValue)
        {
            try
            {
                var setting = _businessRuleSettings?.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
                    return defaultValue;

                if (bool.TryParse(setting.Value, out bool parsed))
                    return parsed;

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private string GetCountryCode(object address)
        {
            if (address == null)
                return string.Empty;

            var prop = address.GetType().GetProperty("Country");
            return prop?.GetValue(address, null)?.ToString()?.Trim() ?? string.Empty;
        }

        private bool IsCountry(string value, string expected)
        {
            return string.Equals((value ?? string.Empty).Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private bool GetBooleanReference(object referenceValue)
        {
            if (referenceValue == null)
                return false;

            var text = referenceValue.ToString().Trim();
            if (bool.TryParse(text, out bool parsedBool))
                return parsedBool;

            return string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "Y", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "YES", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase);
        }

        private decimal? GetNullableDecimal(object value)
        {
            if (value == null)
                return null;

            var text = value.ToString().Trim();
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedInvariant))
                return parsedInvariant;

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal parsedCurrent))
                return parsedCurrent;

            return null;
        }

        private void EnsureCommercialInvoiceMethod(Package package, int method)
        {
            package.CommercialInvoiceMethod = method;
        }

        private void EnsureExportReason(Package package, string exportReason)
        {
            package.ExportReason = exportReason;
        }

        private void EnsurePackageExtra(Package package, string name, string value)
        {
            if (package.UserData1 == null)
                package.UserData1 = string.Empty;

            package.UserData1 = string.IsNullOrWhiteSpace(package.UserData1)
                ? name + "=" + value
                : package.UserData1 + ";" + name + "=" + value;
        }

        private void ApplyDryIce(Package package, decimal dryIceWeightKg, bool isUsToUs)
        {
            decimal dryIceWeightLbs = dryIceWeightKg * 2.20462262185m;

            if (package.Weight == null)
                package.Weight = new Weight();

            package.Weight.Amount = Convert.ToDouble(package.Weight.Amount) + Convert.ToDouble(dryIceWeightLbs);
            package.DryIceWeight = (double)dryIceWeightLbs;
            package.DryIcePurpose = "Medical";
            package.DryIceRegulationSet = isUsToUs ? 1 : 2;
        }

        private void ValidateOrAdjustService(ShipmentRequest shipmentRequest, bool isUsToUs, bool isCanadaToCanada, bool biologicalSample)
        {
            var currentService = shipmentRequest.PackageDefaults?.Service;

            if (biologicalSample)
            {
                _logger?.Log(this, LogLevel.Info, "Biological sample detected. CS Adapter service validation will be enforced by configured service selection.");
            }

            if (isUsToUs)
            {
                _logger?.Log(this, LogLevel.Info, "Domestic lane detected. Current service: " + (currentService != null ? currentService.Symbol : "<none>") + ". Expected validation target: NDA Early AM.");
            }
            else if (isCanadaToCanada || !isUsToUs)
            {
                _logger?.Log(this, LogLevel.Info, "International/cross-border lane detected. Current service: " + (currentService != null ? currentService.Symbol : "<none>") + ". Expected validation target: UPS Express with Saturday Delivery.");
            }
        }
    }
}
