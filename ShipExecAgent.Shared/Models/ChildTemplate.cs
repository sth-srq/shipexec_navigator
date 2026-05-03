namespace ShipExecAgent.Shared.Models;

public class ChildTemplate
{
    public string ElementName { get; set; } = string.Empty;
    public List<ChildField> Fields { get; set; } = [];
}
