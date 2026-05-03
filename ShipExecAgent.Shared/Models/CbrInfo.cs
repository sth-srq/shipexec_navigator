namespace ShipExecAgent.Shared.Models;

public class CbrInfo
{
    public int          Id              { get; set; }
    public string       Name            { get; set; } = string.Empty;
    public string?      Description     { get; set; }
    public string?      Script          { get; set; }
    public string?      Version         { get; set; }
    public List<string> UsedByProfiles  { get; set; } = [];
}
