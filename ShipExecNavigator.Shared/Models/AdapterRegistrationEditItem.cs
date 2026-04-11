namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Represents an adapter registration field edit identified by the AI chat.
/// Each item targets one adapter registration and carries a dictionary of field→value changes.
/// </summary>
public class AdapterRegistrationEditItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field-name → new-value pairs to apply.
    /// Field names are case-insensitive and must match PSI.Sox.AdapterRegistration property names.
    /// </summary>
    public Dictionary<string, string> Edits { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
