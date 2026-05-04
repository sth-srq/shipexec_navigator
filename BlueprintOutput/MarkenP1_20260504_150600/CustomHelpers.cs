using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

private string GetPackageValue(ShipmentRequest shipmentRequest, string key)
{
    if (shipmentRequest == null || shipmentRequest.PackageDefaults == null)
        return null;

    var pd = shipmentRequest.PackageDefaults;
    var prop = pd.GetType().GetProperty(key);
    if (prop != null)
    {
        var val = prop.GetValue(pd, null);
        return val == null ? null : val.ToString();
    }

    var customDataProp = pd.GetType().GetProperty("CustomData");
    if (customDataProp != null)
    {
        var customData = customDataProp.GetValue(pd, null) as IEnumerable<KeyValuePair<string, object>>;
        if (customData != null)
        {
            foreach (var kv in customData)
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value == null ? null : kv.Value.ToString();
        }
    }

    return null;
}

private void SetOrCreateCustomData(ShipmentRequest shipmentRequest, string key, string value)
{
    if (shipmentRequest == null || shipmentRequest.PackageDefaults == null)
        return;

    var pd = shipmentRequest.PackageDefaults;
    var prop = pd.GetType().GetProperty(key);
    if (prop != null && prop.CanWrite)
    {
        object converted = value;
        if (prop.PropertyType == typeof(string)) converted = value;
        else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?)) { int i; int.TryParse(value, out i); converted = i; }
        else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?)) { decimal d; decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out d); converted = d; }
        else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?)) { bool b; bool.TryParse(value, out b); converted = b; }
        prop.SetValue(pd, converted, null);
        return;
    }

    var customDataProp = pd.GetType().GetProperty("CustomData");
    if (customDataProp != null)
    {
        var customData = customDataProp.GetValue(pd, null) as IDictionary<string, object>;
        if (customData != null)
        {
            customData[key] = value;
        }
    }
}

private void SetPackageWeight(object pkg, decimal weight)
{
    if (pkg == null) return;
    var weightProp = pkg.GetType().GetProperty("Weight");
    if (weightProp == null || !weightProp.CanWrite) return;

    if (weightProp.PropertyType == typeof(decimal) || weightProp.PropertyType == typeof(decimal?))
        weightProp.SetValue(pkg, weight, null);
    else if (weightProp.PropertyType == typeof(double) || weightProp.PropertyType == typeof(double?))
        weightProp.SetValue(pkg, (double)weight, null);
    else if (weightProp.PropertyType == typeof(float) || weightProp.PropertyType == typeof(float?))
        weightProp.SetValue(pkg, (float)weight, null);
    else
        weightProp.SetValue(pkg, weight.ToString(CultureInfo.InvariantCulture), null);
}

private void AddOrUpdateShipmentAmount(ShipmentRequest shipmentRequest, string fieldName, string value)
{
    if (shipmentRequest == null || shipmentRequest.PackageDefaults == null)
        return;
    SetOrCreateCustomData(shipmentRequest, fieldName, value);
}

private string GetServiceSymbol(ShipmentRequest shipmentRequest)
{
    if (shipmentRequest == null || shipmentRequest.PackageDefaults == null || shipmentRequest.PackageDefaults.Service == null)
        return null;

    var serviceObj = shipmentRequest.PackageDefaults.Service;
    var prop = serviceObj.GetType().GetProperty("Symbol") ?? serviceObj.GetType().GetProperty("ServiceSymbol") ?? serviceObj.GetType().GetProperty("Name");
    if (prop != null)
    {
        var val = prop.GetValue(serviceObj, null);
        return val == null ? null : val.ToString();
    }
    return serviceObj.ToString();
}

private void SetServiceSymbol(ShipmentRequest shipmentRequest, string serviceSymbol)
{
    if (shipmentRequest == null || shipmentRequest.PackageDefaults == null)
        return;

    if (shipmentRequest.PackageDefaults.Service == null)
        shipmentRequest.PackageDefaults.Service = serviceSymbol;
    else
    {
        var svc = shipmentRequest.PackageDefaults.Service;
        var prop = svc.GetType().GetProperty("Symbol") ?? svc.GetType().GetProperty("ServiceSymbol") ?? svc.GetType().GetProperty("Name");
        if (prop != null && prop.CanWrite)
            prop.SetValue(svc, serviceSymbol, null);
        else
            shipmentRequest.PackageDefaults.Service = serviceSymbol;
    }
}

private string ResolveBiologicalSampleService(ShipmentRequest shipmentRequest, string currentService, string consigneeCountry, string shipperCountry)
{
    bool isDomesticUS = string.Equals(consigneeCountry, "US", StringComparison.OrdinalIgnoreCase)
        && string.Equals(shipperCountry, "US", StringComparison.OrdinalIgnoreCase);

    bool isCanadaToCanada = string.Equals(consigneeCountry, "CA", StringComparison.OrdinalIgnoreCase)
        && string.Equals(shipperCountry, "CA", StringComparison.OrdinalIgnoreCase);

    bool isCrossBorder = !string.IsNullOrEmpty(consigneeCountry)
        && !string.IsNullOrEmpty(shipperCountry)
        && !string.Equals(consigneeCountry, shipperCountry, StringComparison.OrdinalIgnoreCase);

    if (isDomesticUS)
    {
        if (string.IsNullOrEmpty(currentService) || currentService.IndexOf("NDA", StringComparison.OrdinalIgnoreCase) < 0)
            return "UPS NDA Early AM";
        return currentService;
    }

    if (isCanadaToCanada || isCrossBorder)
    {
        if (string.IsNullOrEmpty(currentService) || currentService.IndexOf("Express", StringComparison.OrdinalIgnoreCase) < 0)
            return "UPS Express with Saturday Delivery";
        return currentService;
    }

    return currentService;
}