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

public class BiologicalReturnsShipmentManager
{
    private readonly ILogger _logger;
    private readonly IBusinessObjectApi _businessObjectApi;
    private readonly List<BusinessRuleSetting> _businessRuleSettings;
    private readonly IProfile _profile;
    private readonly ClientContext _clientContext;

    public BiologicalReturnsShipmentManager(ILogger logger, IBusinessObjectApi businessObjectApi, List<BusinessRuleSetting> businessRuleSettings, IProfile profile, ClientContext clientContext)
    {
        _logger = logger;
        _businessObjectApi = businessObjectApi;
        _businessRuleSettings = businessRuleSettings;
        _profile = profile;
        _clientContext = clientContext;
    }

    public void PreShip(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
    {
        if (shipmentRequest == null)
            throw new ArgumentNullException(nameof(shipmentRequest));

        if (shipmentRequest.PackageDefaults == null)
            shipmentRequest.PackageDefaults = new Package();

        if (shipmentRequest.PackageDefaults.Consignee == null)
            shipmentRequest.PackageDefaults.Consignee = new NameAddress();

        if (shipmentRequest.Packages == null || shipmentRequest.Packages.Count == 0)
            return;

        var package = shipmentRequest.Packages[0];
        string temperature = GetPackageString(package, "ConsigneeReference");
        string miscReference3 = GetPackageString(package, "MiscReference3");
        string miscReference4 = GetPackageString(package, "MiscReference4");

        string shipperCountry = GetCountry(shipmentRequest.PackageDefaults.ReturnAddress);
        string consigneeCountry = GetCountry(shipmentRequest.PackageDefaults.Consignee);
        bool isDomesticUS = IsUsCountry(shipperCountry) && IsUsCountry(consigneeCountry);
        bool isCanadaToCanada = IsCanadaCountry(shipperCountry) && IsCanadaCountry(consigneeCountry);
        bool isInternationalReturn = !string.IsNullOrWhiteSpace(shipperCountry) && !string.IsNullOrWhiteSpace(consigneeCountry) && !string.Equals(shipperCountry, consigneeCountry, StringComparison.OrdinalIgnoreCase);
        bool isCrossBorderOrInternational = isInternationalReturn || isCanadaToCanada || (!IsUsCountry(shipperCountry) && !IsUsCountry(consigneeCountry));

        if (isInternationalReturn)
        {
            SetIntegerProperty(package, "CommercialInvoiceMethod", 1);
            SetStringProperty(package, "ExportReason", "Medical");
        }

        bool biologicalSample = ParseBoolean(miscReference4, true);
        EnsureServicePreference(shipmentRequest, "CS Adapter");

        if (isDomesticUS)
        {
            if (!IsServiceValidForLane(shipmentRequest, "NDA Early AM"))
            {
                shipmentRequest.PackageDefaults.Service = CreateService("NDA");
                SetBooleanProperty(package, "SaturdayDelivery", false);
            }
        }
        else if (isCrossBorderOrInternational)
        {
            if (!IsServiceValidForLane(shipmentRequest, "UPS Express"))
            {
                shipmentRequest.PackageDefaults.Service = CreateService("UPS Saver");
                SetBooleanProperty(package, "SaturdayDelivery", false);
            }
        }

        if (biologicalSample)
        {
            SetStringProperty(package, "PackageExtra_RESTRICTED_ARTICLE_TYPE", "32");
        }

        if (!string.IsNullOrWhiteSpace(miscReference3))
        {
            decimal dryIceKg = ParseDecimal(miscReference3, 0m);
            decimal dryIceLbs = ConvertKgToLbs(dryIceKg);

            SetDecimalProperty(package, "DryIceWeight", dryIceLbs);
            SetStringProperty(package, "DryIcePurpose", "Medical");
            SetStringProperty(package, "DryIceRegulationSet", isDomesticUS ? "International Air Transportation Association regulations." : "US 49 CFR regulations.");

            decimal currentWeight = GetDecimalProperty(package, "WeightAmount");
            SetDecimalProperty(package, "WeightAmount", currentWeight + dryIceLbs);
        }
    }

    public ShipmentResponse Ship(ShipmentRequest shipmentRequest, Pickup pickup, bool shipWithoutTransaction, bool print, SerializableDictionary userParams)
    {
        if (!IsPickupFallbackEnabled())
            return null;

        if (shipmentRequest == null)
            return null;

        if (pickup != null)
            return null;

        Pickup fallbackPickup = BuildFallbackPickup(shipmentRequest, userParams);
        if (fallbackPickup == null)
            return null;

        AttachPickupToShipmentRequest(shipmentRequest, fallbackPickup);
        return null;
    }

    private string GetPackageString(PackageRequest package, string propertyName)
    {
        if (package == null)
            return null;

        var prop = package.GetType().GetProperty(propertyName);
        if (prop == null)
            return null;

        object value = prop.GetValue(package);
        return value?.ToString();
    }

    private string GetCountry(NameAddress address)
    {
        if (address == null)
            return null;

        return address.Country;
    }

    private bool IsUsCountry(string country)
    {
        return string.Equals(country, "US", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(country, "USA", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(country, "United States", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCanadaCountry(string country)
    {
        return string.Equals(country, "CA", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(country, "Canada", StringComparison.OrdinalIgnoreCase);
    }

    private bool ParseBoolean(string value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (bool.TryParse(value, out bool parsed))
            return parsed;

        return defaultValue;
    }

    private decimal ParseDecimal(string value, decimal defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        if (decimal.TryParse(value, out decimal parsed))
            return parsed;

        return defaultValue;
    }

    private decimal ConvertKgToLbs(decimal kg)
    {
        return kg * 2.2046226218m;
    }

    private void EnsureServicePreference(ShipmentRequest shipmentRequest, string serviceName)
    {
        if (shipmentRequest.PackageDefaults == null)
            shipmentRequest.PackageDefaults = new Package();

        if (shipmentRequest.PackageDefaults.Service == null || string.IsNullOrWhiteSpace(shipmentRequest.PackageDefaults.Service.Symbol))
            shipmentRequest.PackageDefaults.Service = CreateService(serviceName);
    }

    private bool IsServiceValidForLane(ShipmentRequest shipmentRequest, string serviceName)
    {
        return shipmentRequest?.PackageDefaults?.Service != null && string.Equals(shipmentRequest.PackageDefaults.Service.Symbol, serviceName, StringComparison.OrdinalIgnoreCase);
    }

    private Service CreateService(string symbol)
    {
        return new Service { Symbol = symbol };
    }

    private void SetStringProperty(object target, string propertyName, string value)
    {
        if (target == null)
            return;

        var prop = target.GetType().GetProperty(propertyName);
        if (prop == null || !prop.CanWrite)
            return;

        if (prop.PropertyType == typeof(string))
            prop.SetValue(target, value);
    }

    private void SetIntegerProperty(object target, string propertyName, int value)
    {
        if (target == null)
            return;

        var prop = target.GetType().GetProperty(propertyName);
        if (prop == null || !prop.CanWrite)
            return;

        if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
            prop.SetValue(target, value);
    }

    private void SetDecimalProperty(object target, string propertyName, decimal value)
    {
        if (target == null)
            return;

        var prop = target.GetType().GetProperty(propertyName);
        if (prop == null || !prop.CanWrite)
            return;

        if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
            prop.SetValue(target, value);
    }

    private decimal GetDecimalProperty(object target, string propertyName)
    {
        if (target == null)
            return 0m;

        var prop = target.GetType().GetProperty(propertyName);
        if (prop == null)
            return 0m;

        object value = prop.GetValue(target);
        if (value is decimal dec)
            return dec;

        if (value is double dbl)
            return (decimal)dbl;

        if (value is float flt)
            return (decimal)flt;

        if (value != null && decimal.TryParse(value.ToString(), out decimal parsed))
            return parsed;

        return 0m;
    }

    private void SetBooleanProperty(object target, string propertyName, bool value)
    {
        if (target == null)
            return;

        var prop = target.GetType().GetProperty(propertyName);
        if (prop == null || !prop.CanWrite)
            return;

        if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
            prop.SetValue(target, value);
    }

    private bool IsPickupFallbackEnabled()
    {
        string raw = _businessRuleSettings?.FirstOrDefault(x => string.Equals(x.Key, "PickupFallbackEnabled", StringComparison.OrdinalIgnoreCase))?.Value;
        return ParseBoolean(raw, false);
    }

    private Pickup BuildFallbackPickup(ShipmentRequest shipmentRequest, SerializableDictionary userParams)
    {
        return new Pickup();
    }

    private void AttachPickupToShipmentRequest(ShipmentRequest shipmentRequest, Pickup pickup)
    {
        if (shipmentRequest == null || pickup == null)
            return;

        var prop = shipmentRequest.GetType().GetProperty("Pickup");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(shipmentRequest, pickup);
            return;
        }

        prop = shipmentRequest.GetType().GetProperty("PickupRequest");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(shipmentRequest, pickup);
        }
    }
}
