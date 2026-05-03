namespace ShipExecAgent.Shared.Models;

public class TemplateInfo
{
    public int     Id           { get; set; }
    public string  TemplateName { get; set; } = string.Empty;
    public string  TemplateType { get; set; } = string.Empty;
    public string? TemplateData { get; set; }
}
