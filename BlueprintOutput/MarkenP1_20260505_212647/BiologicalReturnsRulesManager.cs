using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using PSI.Sox;
using PSI.Sox.Interfaces;
using PSI.Sox.Configuration;
using PSI.Sox.Data;
using PSI.Sox.Licensing;
using PSI.Sox.ML;
using PSI.Sox.Print;
using PSI.Sox.Resources;
using PSI.Sox.Wcf;

public class BiologicalReturnsRulesManager {
    private readonly ILogger _logger;
    private readonly IBusinessObjectApi _api;
    private readonly IProfile _profile;
    private readonly List<BusinessRuleSetting> _settings;
    private readonly ClientContext _clientContext;

    /// <summary>
    /// Creates the manager used by the PreShip hook to enforce shipment rules.
    /// </summary>
    public BiologicalReturnsRulesManager(ILogger logger, IBusinessObjectApi api, IProfile profile, List<BusinessRuleSetting> settings, ClientContext clientContext) {
        // Store logger for tracing the rule flow and any validation failures.
        _logger = logger;
        // Store ShipExec API access for any carrier/profile/history lookups needed by rules.
        _api = api;
        // Store the profile so service, shipper, and carrier metadata can be resolved.
        _profile = profile;
        // Store Commander settings so all routing/service keys remain configurable.
        _settings = settings;
        // Store the client context so downstream helpers can make user-aware decisions.
        _clientContext = clientContext;
    }

    /// <summary>
    /// Enforces the biological returns shipment requirements before ship execution.
    /// This method mutates the shipment request in place and throws on invalid data.
    /// </summary>
    public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams) {
        _logger.LogInfo(this, "SBR", "PreShip", "Start", "Beginning Marken biological returns enforcement");
        if (shipmentRequest == null) { throw new Exception("ShipmentRequest is required for Marken return processing."); }
        if (shipmentRequest.PackageDefaults == null) { shipmentRequest.PackageDefaults = new Package(); }
        if (shipmentRequest.PackageDefaults.Consignee == null) { shipmentRequest.PackageDefaults.Consignee = new NameAddress(); }
        if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0) { shipmentRequest.Packages = new List<PackageRequest> { new PackageRequest() }; }
        string pickupCountry = shipmentRequest.PackageDefaults.Consignee.Country == null ? string.Empty : shipmentRequest.PackageDefaults.Consignee.Country.ToString().Trim().ToUpperInvariant();
        string shipFromCountry = string.Empty;
        if (shipmentRequest.PackageDefaults.OriginAddress != null && !string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.OriginAddress.Country)) { shipFromCountry = shipmentRequest.PackageDefaults.OriginAddress.Country.Trim().ToUpperInvariant(); }
        bool isCrossBorder = !string.IsNullOrWhiteSpace(pickupCountry) && !string.IsNullOrWhiteSpace(shipFromCountry) && !string.Equals(pickupCountry, shipFromCountry, StringComparison.OrdinalIgnoreCase);
        string temperature = shipmentRequest.PackageDefaults.ConsigneeReference == null ? string.Empty : shipmentRequest.PackageDefaults.ConsigneeReference.ToString().Trim();
        string biologicalFlag = shipmentRequest.PackageDefaults.MiscReference4 == null ? string.Empty : shipmentRequest.PackageDefaults.MiscReference4.ToString().Trim();
        string dryIceKgText = shipmentRequest.PackageDefaults.MiscReference3 == null ? string.Empty : shipmentRequest.PackageDefaults.MiscReference3.ToString().Trim();
        bool isBiologicalSample = string.Equals(biologicalFlag, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(biologicalFlag, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(biologicalFlag, "yes", StringComparison.OrdinalIgnoreCase);
        if (isCrossBorder) { shipmentRequest.PackageDefaults.CommercialInvoiceMethod = 1; shipmentRequest.PackageDefaults.ExportReason = "Medical"; }
        if (isBiologicalSample) {
            if (shipmentRequest.PackageDefaults.PackageExtras == null) { shipmentRequest.PackageDefaults.PackageExtras = new SerializableDictionary(); }
            shipmentRequest.PackageDefaults.PackageExtras["RESTRICTED_ARTICLE_TYPE"] = "32";
        }
        if (!string.IsNullOrWhiteSpace(dryIceKgText)) {
            double dryIceKg;
            if (!double.TryParse(dryIceKgText, out dryIceKg)) { throw new Exception("Dry Ice Weight must be numeric when Temperature is Frozen."); }
            double dryIceLb = dryIceKg * 2.2046226218d;
            if (shipmentRequest.PackageDefaults.DryIceWeight == null) { shipmentRequest.PackageDefaults.DryIceWeight = new Weight(); }
            shipmentRequest.PackageDefaults.DryIceWeight.Amount = dryIceLb;
            shipmentRequest.PackageDefaults.DryIceWeight.Units = "LB";
            if (shipmentRequest.Packages[0].Weight == null) { shipmentRequest.Packages[0].Weight = new Weight(); }
            shipmentRequest.Packages[0].Weight.Amount = shipmentRequest.Packages[0].Weight.Amount + dryIceLb;
            if (string.IsNullOrWhiteSpace(shipmentRequest.Packages[0].Weight.Units)) { shipmentRequest.Packages[0].Weight.Units = "LB"; }
            shipmentRequest.Packages[0].DryIcePurpose = 0;
            shipmentRequest.Packages[0].DryIceRegulationSet = string.Equals(pickupCountry, "US", StringComparison.OrdinalIgnoreCase) && string.Equals(shipFromCountry, "US", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        }
        ApplyServiceRouting(shipmentRequest, pickupCountry, shipFromCountry, isCrossBorder, isBiologicalSample);
        _logger.LogInfo(this, "SBR", "PreShip", "End", "Completed Marken biological returns enforcement");
    }

    /// <summary>
    /// Resolves the service symbol required by the blueprint for the active lane.
    /// This helper isolates routing decisions so the main rule method remains readable.
    /// </summary>
    private void ApplyServiceRouting(ShipmentRequest shipmentRequest, string pickupCountry, string shipFromCountry, bool isCrossBorder, bool isBiologicalSample) {
        string domesticPreferred = GetSetting("BiologicalReturns.Service.USDomesticPreferred", "CONNECTSHIP_UPS.UPS.NDA");
        string domesticFallback = GetSetting("BiologicalReturns.Service.USDomesticFallback", "CONNECTSHIP_UPS.UPS.NDA");
        string internationalPreferred = GetSetting("BiologicalReturns.Service.InternationalPreferred", "CONNECTSHIP_UPS.UPS.EXP");
        string internationalFallback = GetSetting("BiologicalReturns.Service.InternationalFallback", "CONNECTSHIP_UPS.UPS.SVR");
        string targetServiceSymbol = isCrossBorder ? internationalPreferred : domesticPreferred;
        if (isBiologicalSample) { targetServiceSymbol = isCrossBorder ? internationalPreferred : domesticPreferred; }
        if (shipmentRequest.PackageDefaults.Service == null) { shipmentRequest.PackageDefaults.Service = new Service(); }
        shipmentRequest.PackageDefaults.Service.Symbol = targetServiceSymbol;
        bool needsFallback = string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Service.Symbol);
        if (needsFallback) { shipmentRequest.PackageDefaults.Service.Symbol = isCrossBorder ? internationalFallback : domesticFallback; }
        if (string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Shipper) && _profile != null && _profile.Shippers != null && _profile.Shippers.Count > 0) { shipmentRequest.PackageDefaults.Shipper = _profile.Shippers[0].Name; }
        if (string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Terms)) { shipmentRequest.PackageDefaults.Terms = "Prepaid"; }
    }

    /// <summary>
    /// Reads a business rule setting by key and falls back to a default value.
    /// </summary>
    private string GetSetting(string key, string defaultValue) {
        var tools = new Tools(_logger);
        string value = tools.GetStringValueFromBusinessRuleSettings(key, _settings);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
