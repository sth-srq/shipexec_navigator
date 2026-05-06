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

public class PickupAssociationManager {
    private readonly ILogger _logger;
    private readonly IBusinessObjectApi _api;
    private readonly IProfile _profile;
    private readonly List<BusinessRuleSetting> _settings;
    private readonly ClientContext _clientContext;

    /// <summary>
    /// Creates the pickup manager used by the Ship hook.
    /// </summary>
    public PickupAssociationManager(ILogger logger, IBusinessObjectApi api, IProfile profile, List<BusinessRuleSetting> settings, ClientContext clientContext) {
        // Store the logger so we can trace pickup fallback decisions.
        _logger = logger;
        // Store the API in case the implementation later needs to create or persist a pickup record.
        _api = api;
        // Store the profile so pickup defaults can be derived from user/site context.
        _profile = profile;
        // Store the configurable settings so pickup behavior can be controlled without code changes.
        _settings = settings;
        // Store the client context so user-specific fallback data can be resolved.
        _clientContext = clientContext;
    }

    /// <summary>
    /// Provides a fallback pickup association path for the Ship hook.
    /// Returning null allows ShipExec to continue with normal shipping.
    /// </summary>
    public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams) {
        _logger.LogInfo(this, "SBR", "Ship", "Start", "Evaluating server-side pickup fallback");
        bool enabled = string.Equals(GetSetting("BiologicalReturns.PickupFallbackEnabled", "false"), "true", StringComparison.OrdinalIgnoreCase);
        if (!enabled) { return null; }
        if (pickup != null) { return null; }
        Pickup fallbackPickup = BuildPickupFromShipment(shipmentRequest);
        if (fallbackPickup == null) { return null; }
        _logger.LogInfo(this, "SBR", "Ship", "End", "Pickup fallback prepared; allowing default ship flow to continue");
        return null;
    }

    /// <summary>
    /// Builds a pickup object from shipment and profile data.
    /// </summary>
    private Pickup BuildPickupFromShipment(ShipmentRequest shipmentRequest) {
        if (shipmentRequest == null || shipmentRequest.PackageDefaults == null || shipmentRequest.PackageDefaults.Consignee == null) {
            return null;
        }
        Pickup pickup = new Pickup();
        pickup.PickupAddress = shipmentRequest.PackageDefaults.Consignee;
        pickup.DestinationCountryCode = shipmentRequest.PackageDefaults.Consignee.Country;
        pickup.PickupReferenceNumber = BuildPickupReference(shipmentRequest);
        return pickup;
    }

    /// <summary>
    /// Builds a readable pickup reference from shipment values.
    /// </summary>
    private string BuildPickupReference(ShipmentRequest shipmentRequest) {
        string prefix = GetSetting("BiologicalReturns.PickupDefaultReferencePrefix", "MARKEN");
        string shipRef = shipmentRequest != null && shipmentRequest.PackageDefaults != null && !string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.ShipperReference) ? shipmentRequest.PackageDefaults.ShipperReference : "UNKNOWN";
        return prefix + "-" + shipRef;
    }

    /// <summary>
    /// Reads a business rule setting with a fallback default value.
    /// </summary>
    private string GetSetting(string key, string defaultValue) {
        var tools = new Tools(_logger);
        string value = tools.GetStringValueFromBusinessRuleSettings(key, _settings);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
