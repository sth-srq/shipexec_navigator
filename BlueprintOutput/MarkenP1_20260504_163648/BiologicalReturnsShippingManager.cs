using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

public class BiologicalReturnsShippingManager
{
    public BiologicalReturnsShippingManager(object logger, object businessObjectApi, object profile, List<BusinessRuleSetting> settings, object clientContext)
    {
    }

    public void PreShip(object shipmentRequest, object userParams)
    {
        if (shipmentRequest == null)
            return;

        object packageDefaults = GetIfExists(shipmentRequest, "PackageDefaults");
        if (packageDefaults == null)
            return;

        object consignee = GetIfExists(packageDefaults, "Consignee");
        object shipper = GetIfExists(packageDefaults, "Shipper");
        string pickupCountry = GetString(consignee, "Country");
        string shipToCountry = GetString(shipper, "Country");
        string temperature = GetPackageDefaultString(shipmentRequest, "ConsigneeReference");
        bool isBiologicalSample = IsTrue(GetPackageDefaultString(shipmentRequest, "MiscReference4"));

        if (!string.IsNullOrWhiteSpace(pickupCountry) && !string.IsNullOrWhiteSpace(shipToCountry) && !string.Equals(pickupCountry, shipToCountry, StringComparison.OrdinalIgnoreCase))
        {
            SetIfExists(packageDefaults, "CommercialInvoiceMethod", 1);
            SetIfExists(packageDefaults, "ExportReason", "Medical");
        }

        if (isBiologicalSample)
        {
            EnsurePackageExtra(shipmentRequest, "RESTRICTED_ARTICLE_TYPE", "32");
        }

        object firstPackage = GetFirstPackage(shipmentRequest);
        if (firstPackage != null)
        {
            ApplyDryIceIfNeeded(shipmentRequest, firstPackage, temperature, pickupCountry, shipToCountry);
        }

        ApplyServiceRules(shipmentRequest, pickupCountry, shipToCountry, isBiologicalSample);
    }

    private void ApplyServiceRules(object shipmentRequest, string pickupCountry, string shipToCountry, bool isBiologicalSample)
    {
        if (shipmentRequest == null)
            return;

        object packageDefaults = GetIfExists(shipmentRequest, "PackageDefaults");
        if (packageDefaults == null)
            return;

        bool isDomesticUsToUs = string.Equals(pickupCountry, "US", StringComparison.OrdinalIgnoreCase) && string.Equals(shipToCountry, "US", StringComparison.OrdinalIgnoreCase);
        bool isCanadaToCanada = string.Equals(pickupCountry, "CA", StringComparison.OrdinalIgnoreCase) && string.Equals(shipToCountry, "CA", StringComparison.OrdinalIgnoreCase);

        if (isDomesticUsToUs)
        {
            SetIfExists(packageDefaults, "Service", GetIfExists(packageDefaults, "Service") ?? "NDA without Saturday Delivery");
        }
        else if (isCanadaToCanada || (!string.IsNullOrWhiteSpace(pickupCountry) && !string.IsNullOrWhiteSpace(shipToCountry) && !string.Equals(pickupCountry, shipToCountry, StringComparison.OrdinalIgnoreCase)))
        {
            SetIfExists(packageDefaults, "Service", GetIfExists(packageDefaults, "Service") ?? "UPS Saver without Saturday Delivery");
        }

        if (isBiologicalSample)
        {
            SetIfExists(packageDefaults, "SaturdayDelivery", true);
        }
    }

    private void ApplyDryIceIfNeeded(object shipmentRequest, object packageRequest, string temperature, string pickupCountry, string shipToCountry)
    {
        if (!string.Equals(temperature, "Frozen", StringComparison.OrdinalIgnoreCase))
            return;

        string dryIceKgValue = GetPackageDefaultString(shipmentRequest, "MiscReference3");
        if (string.IsNullOrWhiteSpace(dryIceKgValue))
            return;

        if (!decimal.TryParse(dryIceKgValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dryIceKg) &&
            !decimal.TryParse(dryIceKgValue, NumberStyles.Any, CultureInfo.CurrentCulture, out dryIceKg))
        {
            return;
        }

        decimal dryIceLbs = Math.Round(dryIceKg * 2.2046226218m, 2, MidpointRounding.AwayFromZero);
        decimal currentWeight = GetDecimal(packageRequest, "Weight");
        SetIfExists(packageRequest, "Weight", currentWeight + dryIceLbs);
        SetIfExists(packageRequest, "DryIceWeight", dryIceLbs);
        SetIfExists(packageRequest, "DryIcePurpose", "Medical");
        SetIfExists(packageRequest, "DryIceRegulationSet", IsUsToUs(pickupCountry, shipToCountry) ? "International Air Transportation Association regulations" : "US 49 CFR regulations");
    }

    private bool IsUsToUs(string pickupCountry, string shipToCountry)
    {
        return string.Equals(pickupCountry, "US", StringComparison.OrdinalIgnoreCase) && string.Equals(shipToCountry, "US", StringComparison.OrdinalIgnoreCase);
    }

    private string GetPackageDefaultString(object shipmentRequest, string propertyName)
    {
        object packageDefaults = GetIfExists(shipmentRequest, "PackageDefaults");
        return packageDefaults == null ? null : GetString(packageDefaults, propertyName);
    }

    private bool IsTrue(string value)
    {
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsurePackageExtra(object shipmentRequest, string key, string value)
    {
        if (shipmentRequest == null)
            return;

        object packageDefaults = GetIfExists(shipmentRequest, "PackageDefaults");
        if (packageDefaults == null)
            return;

        object packageExtras = GetIfExists(packageDefaults, "PackageExtras");
        if (packageExtras == null)
        {
            packageExtras = new Dictionary<string, object>();
            SetIfExists(packageDefaults, "PackageExtras", packageExtras);
        }

        if (packageExtras is IDictionary dict)
        {
            dict[key] = value;
        }
    }

    private object GetIfExists(object target, string propertyName)
    {
        if (target == null || string.IsNullOrWhiteSpace(propertyName))
            return null;

        PropertyInfo prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return prop == null ? null : prop.GetValue(target, null);
    }

    private void SetIfExists(object target, string propertyName, object value)
    {
        if (target == null || string.IsNullOrWhiteSpace(propertyName))
            return;

        PropertyInfo prop = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null || !prop.CanWrite)
            return;

        try
        {
            if (value == null)
            {
                prop.SetValue(target, null, null);
                return;
            }

            Type targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (targetType.IsAssignableFrom(value.GetType()))
            {
                prop.SetValue(target, value, null);
                return;
            }

            prop.SetValue(target, Convert.ChangeType(value, targetType), null);
        }
        catch
        {
        }
    }

    private decimal GetDecimal(object target, string propertyName)
    {
        object value = GetIfExists(target, propertyName);
        if (value == null)
            return 0m;

        decimal result;
        if (decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            return result;
        if (decimal.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture), NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            return result;
        return 0m;
    }

    private string GetString(object target, string propertyName)
    {
        object value = GetIfExists(target, propertyName);
        return value == null ? null : value.ToString();
    }

    private object GetFirstPackage(object shipmentRequest)
    {
        object packages = GetIfExists(shipmentRequest, "Packages");
        if (packages is IEnumerable enumerable)
        {
            foreach (object item in enumerable)
                return item;
        }
        return null;
    }
}
