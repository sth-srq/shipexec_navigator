namespace ShipExecAgent.Tools.EnvironmentMapper;

/// <summary>
/// Represents a single ID mapping between two environment XML files.
/// </summary>
public record IdMapping(
    string EntityPath,
    string DisplayName,
    string FieldName,
    string File1Value,
    string File2Value)
{
    public bool IsSame => string.Equals(File1Value, File2Value, StringComparison.OrdinalIgnoreCase);
}
