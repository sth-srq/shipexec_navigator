namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Complete entity index for a connected company.
/// Built from the ShipExec API at connection time, this index provides
/// entity data for ALL categories regardless of which tree nodes have been expanded
/// in the UI.  The compact <see cref="ManifestJson"/> is included in the AI system
/// prompt so the model is immediately aware of every entity, while full details are
/// served on-demand through Semantic Kernel plugin functions from
/// <see cref="CategoryDetailsJson"/>.
/// </summary>
public class CompanyEntityIndex
{
    /// <summary>
    /// Compact JSON manifest listing all categories and their entities (id + name only).
    /// Example: <c>{"Shippers":[{"id":"1","name":"UPS"},...],"Profiles":[...]}</c>
    /// </summary>
    public string ManifestJson { get; set; } = "{}";

    /// <summary>
    /// Full entity details by category key.
    /// Key = category name (e.g. "Shippers"), Value = JSON array of all scalar fields.
    /// </summary>
    public Dictionary<string, string> CategoryDetailsJson { get; set; } = new();
}
