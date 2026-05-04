using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class ReturnShipmentManager
{
    private readonly ILogger Logger;
    private readonly IBusinessObjectApi BusinessObjectApi;
    private readonly List<BusinessRuleSetting> BusinessRuleSettings;
    private readonly IProfile Profile;
    private readonly ClientContext ClientContext;
    private readonly Tools Tools;

    public ReturnShipmentManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
    {
        Logger = logger;
        BusinessObjectApi = businessObjectApi;
        BusinessRuleSettings = businessRuleSettings;
        Profile = profile;
        ClientContext = clientContext;
        Tools = new Tools(Logger);
    }

    public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
    {
        if (shipmentRequest == null) throw new ArgumentNullException(nameof(shipmentRequest));
        if (shipmentRequest.PackageDefaults == null) throw new Exception("Shipment is missing package defaults.");

        Logger?.Info("ReturnShipmentManager.PreShip started.");

        EnsurePackageDefaults(shipmentRequest);

        var pkg = shipmentRequest.PackageDefaults;
        var fromCountry = GetCountry(shipmentRequest, true);
        var toCountry = GetCountry(shipmentRequest, false);
        bool isInternationalReturn = !string.IsNullOrWhiteSpace(fromCountry) && !string.IsNullOrWhiteSpace(toCountry) && !string.Equals(fromCountry, toCountry, StringComparison.OrdinalIgnoreCase);
        bool isBiologicalSample = GetBool(pkg.MiscReference4);
        string temperature = (pkg.ConsigneeReference ?? string.Empty).Trim();

        if (isInternationalReturn)
        {
            SetCommercialInvoiceAndExportReason(pkg);
        }

        if (isBiologicalSample)
        {
            SetPackageExtra(pkg, "RESTRICTED_ARTICLE_TYPE", "32");
        }

        ApplyDryIceRules(shipmentRequest, pkg, fromCountry, toCountry, temperature);
        ApplyCarrierAndServiceRules(shipmentRequest, pkg, fromCountry, toCountry, isBiologicalSample);

        Logger?.Info("ReturnShipmentManager.PreShip completed.");
    }

    public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
    {
        Logger?.Info("ReturnShipmentManager.Ship started.");

        var pickupFallbackEnabled = GetSettingBool("ReturnShip_PickupFallbackEnabled", true);
        if (pickupFallbackEnabled && pickup == null)
        {
            pickup = BuildPickupFromUserProfile();
        }

        Logger?.Info("ReturnShipmentManager.Ship delegating to default ShipExec behavior.");
        return null;
    }

    private void EnsurePackageDefaults(ShipmentRequest shipmentRequest)
    {
        if (shipmentRequest.PackageDefaults == null)
            shipmentRequest.PackageDefaults = new PackageRequest();
        if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
            shipmentRequest.Packages = new List<PackageRequest> { new PackageRequest() };
        if (shipmentRequest.Packages[0] == null)
            shipmentRequest.Packages[0] = new PackageRequest();
    }

    private string GetCountry(ShipmentRequest shipmentRequest, bool from)
    {
        var addr = from ? shipmentRequest.PackageDefaults.Shipper : shipmentRequest.PackageDefaults.Consignee;
        return addr?.Country?.Trim();
    }

    private void SetCommercialInvoiceAndExportReason(PackageRequest pkg)
    {
        pkg.CommercialInvoiceMethod = 1;
        pkg.ExportReason = "Medical";
    }

    private void ApplyDryIceRules(ShipmentRequest shipmentRequest, PackageRequest pkg, string fromCountry, string toCountry, string temperature)
    {
        string dryIceKgText = (pkg.MiscReference3 ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(dryIceKgText)) return;

        if (!decimal.TryParse(dryIceKgText, NumberStyles.Any, CultureInfo.InvariantCulture, out var kg) &&
            !decimal.TryParse(dryIceKgText, NumberStyles.Any, CultureInfo.CurrentCulture, out kg))
        {
            throw new Exception("Dry Ice Weight must be a valid numeric value in KG.");
        }

        var lbs = kg * 2.2046226218m;
        if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
            shipmentRequest.Packages = new List<PackageRequest> { pkg };

        pkg.DryIceWeight = lbs;
        pkg.DryIcePurpose = "Medical";
        pkg.DryIceWeightUnit = "LB";
        pkg.PackageWeight = (pkg.PackageWeight ?? 0m) + lbs;

        bool isUsToUs = string.Equals(fromCountry, "US", StringComparison.OrdinalIgnoreCase) && string.Equals(toCountry, "US", StringComparison.OrdinalIgnoreCase);
        pkg.DryIceRegulationSet = isUsToUs
            ? "International Air Transportation Association regulations."
            : "US 49 CFR regulations.";
    }

    private void ApplyCarrierAndServiceRules(ShipmentRequest shipmentRequest, PackageRequest pkg, string fromCountry, string toCountry, bool isBiologicalSample)
    {
        bool domesticUsToUs = string.Equals(fromCountry, "US", StringComparison.OrdinalIgnoreCase) && string.Equals(toCountry, "US", StringComparison.OrdinalIgnoreCase);
        bool international = !domesticUsToUs;

        if (isBiologicalSample)
        {
            var csService = Tools.GetStringValueFromBusinessRuleSettings("ReturnShip_DefaultServiceInternational", BusinessRuleSettings);
            if (!string.IsNullOrWhiteSpace(csService))
                shipmentRequest.PackageDefaults.Service = csService;
            return;
        }

        if (domesticUsToUs)
        {
            if (!IsServiceValidByRateShop(shipmentRequest, "NDA Early AM"))
            {
                shipmentRequest.PackageDefaults.Service = GetSettingOrDefault("ReturnShip_DefaultServiceDomestic", "NDA");
                pkg.SaturdayDelivery = false;
            }
        }
        else if (international)
        {
            if (!IsServiceValidByRateShop(shipmentRequest, "UPS Express Saturday"))
            {
                shipmentRequest.PackageDefaults.Service = GetSettingOrDefault("ReturnShip_DefaultServiceInternational", "UPS Saver");
                pkg.SaturdayDelivery = false;
            }
        }
    }

    private bool IsServiceValidByRateShop(ShipmentRequest shipmentRequest, string serviceName)
    {
        try
        {
            var services = new List<Service>();
            return true;
        }
        catch (Exception ex)
        {
            Logger?.Warning($"Rate shop validation failed for {serviceName}: {ex.Message}");
            return false;
        }
    }

    private void SetPackageExtra(PackageRequest pkg, string key, string value)
    {
        if (pkg.PackageExtras == null)
            pkg.PackageExtras = new SerializableDictionary();
        pkg.PackageExtras[key] = value;
    }

    private Pickup BuildPickupFromUserProfile()
    {
        try
        {
            var pickup = new Pickup();
            var user = Profile?.UserInformation;
            if (user != null)
            {
                pickup.Company = user.Company;
                pickup.Contact = user.Name;
                pickup.Address1 = user.Address1;
                pickup.Address2 = user.Address2;
                pickup.City = user.City;
                pickup.StateProvince = user.StateProvince;
                pickup.PostalCode = user.PostalCode;
                pickup.Country = user.Country;
                pickup.Phone = user.Phone;
            }
            return pickup;
        }
        catch (Exception ex)
        {
            Logger?.Error($"Failed to build pickup from user profile: {ex}");
            return null;
        }
    }

    private string GetSettingOrDefault(string key, string defaultValue)
    {
        var value = Tools.GetStringValueFromBusinessRuleSettings(key, BusinessRuleSettings);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private bool GetSettingBool(string key, bool defaultValue)
    {
        var value = Tools.GetStringValueFromBusinessRuleSettings(key, BusinessRuleSettings);
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (bool.TryParse(value, out var b)) return b;
        return defaultValue;
    }

    private bool GetBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}

public class Tools
{
    private readonly ILogger Logger;
    public Tools(ILogger logger) { Logger = logger; }

    public string GetStringValueFromBusinessRuleSettings(string key, List<BusinessRuleSetting> settings)
    {
        return settings?.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}