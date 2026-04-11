namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Represents a client to be deleted, as identified by the AI chat.
/// </summary>
public class ClientDeleteItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
