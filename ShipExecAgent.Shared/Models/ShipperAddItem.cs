namespace ShipExecAgent.Shared.Models;

/// <summary>
/// Represents a new shipper to be added, as identified by the AI chat.
/// Field values are optional; the AddShipperDialog will display them
/// pre-populated and let the user confirm or edit before committing.
/// </summary>
public class ShipperAddItem
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string Address2 { get; set; } = string.Empty;
    public string Address3 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Sms { get; set; } = string.Empty;
    public bool PoBox { get; set; }
    public bool Residential { get; set; }
}
