namespace ShipExecAgent.Shared.Models;

/// <summary>
/// Represents a log entry identified by the AI chat as matching a find/search query.
/// </summary>
public class LogFindItem
{
    public int Id { get; set; }
    /// <summary>"App" or "Security"</summary>
    public string Source { get; set; } = string.Empty;
}
