namespace ShipExecAgent.Shared.Models;

public class RequestInfo
{
    public string EntityName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string RequestJson { get; set; } = string.Empty;
}
