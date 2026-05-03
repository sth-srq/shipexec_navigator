namespace ShipExecAgent.Shared.Models;

public class CbrSaveResult
{
    public string  FileName    { get; set; } = string.Empty;
    public string  CompanyName { get; set; } = string.Empty;
    public string  RuleName    { get; set; } = string.Empty;
    public bool    Success     { get; set; }
    public string? Error       { get; set; }
}
