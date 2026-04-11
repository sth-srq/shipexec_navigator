namespace ShipExecNavigator.Shared.Models;

public class ApplyResultItem
{
    public string EntityName { get; set; } = string.Empty;
    public string Operation  { get; set; } = string.Empty;
    public string Endpoint   { get; set; } = string.Empty;
    public bool   Success    { get; set; }
    public string Message    { get; set; } = string.Empty;
}
