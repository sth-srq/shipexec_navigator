namespace ShipExecAgent.Shared.Models;

public class XmlAttributeViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsNamespaceDeclaration { get; set; }
    public string NamespacePrefix { get; set; } = string.Empty;
    
    // Track original state for export
    public string OriginalValue { get; set; } = string.Empty;
    public bool IsModified { get; set; }
}
