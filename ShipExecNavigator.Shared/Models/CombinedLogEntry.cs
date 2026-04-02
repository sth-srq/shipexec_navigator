namespace ShipExecNavigator.Shared.Models;

public class CombinedLogEntry
{
    /// <summary>"App" or "Security"</summary>
    public string Source { get; set; } = string.Empty;

    public int Id { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string? Logger { get; set; }
    public DateTime? LogDate { get; set; }
    public string? Message { get; set; }
    public string? TransactionId { get; set; }
    public string? ServerAddress { get; set; }

    // Security-specific (null for App entries)
    public string? UserName { get; set; }
    public string? ActionType { get; set; }
    public string? Entity { get; set; }
    public string? EntityName { get; set; }
    public string? CurrentValue { get; set; }
    public string? PreviousValue { get; set; }
}
