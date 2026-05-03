namespace ShipExecAgent.Shared.Models;

public class SbrInfo
{
    public int          Id              { get; set; }
    public string       Name            { get; set; } = string.Empty;
    public string?      Description     { get; set; }
    public string?      Version         { get; set; }
    public string?      Author          { get; set; }
    public string?      AuthorEmail     { get; set; }
    public List<string> UsedByProfiles  { get; set; } = [];
}
