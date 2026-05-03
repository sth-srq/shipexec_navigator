namespace ShipExecAgent.Shared.Models;

public class TemplateSaveResult
{
    public string  FileName     { get; set; } = string.Empty;
    public string  CompanyId    { get; set; } = string.Empty;
    public string  TemplateName { get; set; } = string.Empty;
    public string  TemplateType { get; set; } = string.Empty;
    public bool    Success      { get; set; }
    public string? Error        { get; set; }
}
