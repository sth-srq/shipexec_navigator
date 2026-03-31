namespace ShipExecNavigator.Shared.Models;

public class EnumOption
{
    /// <summary>The value stored in the XML (numeric string for enums, "true"/"false" for booleans).</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>The human-readable label shown in the dropdown.</summary>
    public string Display { get; set; } = string.Empty;
}
