namespace ShipExecAgent.Shared.Models;

/// <summary>
/// Represents a shipper field edit identified by the AI chat.
/// Each item targets one shipper and carries a dictionary of field→value changes.
/// </summary>
public class ShipperEditItem
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field-name → new-value pairs to apply (e.g. { "Address1": "Test Street" }).
    /// Field names are case-insensitive and must match PSI.Sox.Shipper property names.
    /// </summary>
    public Dictionary<string, string> Edits { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
