using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class PackageExtra
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class Weight
{
    public decimal Amount { get; set; }
}

public class PackageRequest
{
    public Weight Weight { get; set; }
    public List<PackageExtra> PackageExtras { get; set; }
}

public class NameAddress
{
    public string Company { get; set; }
    public string Contact { get; set; }
    public string Address1 { get; set; }
    public string Address2 { get; set; }
    public string Address3 { get; set; }
    public string City { get; set; }
    public string StateProvince { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
    public string Phone { get; set; }
}

public class PackageRequestDefaults
{
    public NameAddress Consignee { get; set; }
    public NameAddress Shipper { get; set; }
    public string ConsigneeReference { get; set; }
    public string ShipperReference { get; set; }
    public string MiscReference1 { get; set; }
    public string MiscReference2 { get; set; }
    public string MiscReference3 { get; set; }
    public string MiscReference4 { get; set; }
    public string Service { get; set; }
    public string Terms { get; set; }
    public bool SaturdayDelivery { get; set; }
    public string Description { get; set; }
    public string DryIceWeightUnits { get; set; }
    public bool ReturnDelivery { get; set; }
    public int CommercialInvoiceMethod { get; set; }
    public string ExportReason { get; set; }
    public string DryIceWeight { get; set; }
    public string DryIcePurpose { get; set; }
    public string DryIceRegulationSet { get; set; }
}

public class ShipmentRequest
{
    public PackageRequestDefaults PackageDefaults { get; set; }
    public List<PackageRequest> Packages { get; set; }
}

public class ShipmentResponse
{
    public PackageRequestDefaults PackageDefaults { get; set; }
}

public class Pickup { }

public class SerializableDictionary : Dictionary<string, object> { }
