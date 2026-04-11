namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Represents a shipper to be deleted, as identified by the AI chat.
/// </summary>
public class ShipperDeleteItem
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
