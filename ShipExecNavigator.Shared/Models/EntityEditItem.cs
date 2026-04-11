namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Represents a generic entity field edit identified by the AI chat.
/// Works for any XML entity type on the Navigator screen (Profile, Site,
/// CarrierRoute, DataConfigurationMapping, DocumentConfiguration, Machine,
/// PrinterConfiguration, PrinterDefinition, ScaleConfiguration, Schedule,
/// SourceConfiguration, etc.).
/// </summary>
public class EntityEditItem
{
    /// <summary>
    /// The singular entity type name (e.g. "Profile", "Site", "CarrierRoute").
    /// Must match the XML element name used in the tree.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Field-name → new-value pairs to apply.
    /// Field names are case-insensitive and must match child XML element names.
    /// </summary>
    public Dictionary<string, string> Edits { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
