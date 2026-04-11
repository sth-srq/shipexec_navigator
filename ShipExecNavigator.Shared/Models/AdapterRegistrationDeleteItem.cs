namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Represents an adapter registration to be deleted, as identified by the AI chat.
/// </summary>
public class AdapterRegistrationDeleteItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
