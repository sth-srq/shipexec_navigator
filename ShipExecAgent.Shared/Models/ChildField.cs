namespace ShipExecAgent.Shared.Models;

public class ChildField
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<EnumOption>? AllowedValues { get; set; }
}
