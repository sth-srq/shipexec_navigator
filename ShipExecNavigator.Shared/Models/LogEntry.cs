namespace ShipExecNavigator.Shared.Models;

public class LogEntry
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string Logger { get; set; } = string.Empty;
    public DateTime? LogDate { get; set; }
    public string? Message { get; set; }
    public string? CompanyId { get; set; }
    public string? TransactionId { get; set; }
    public string? ServerAddress { get; set; }
    public string? EnterpriseId { get; set; }
    public string? SiteId { get; set; }
    public string? ClientAddress { get; set; }
    public string? UserAgentString { get; set; }
    public string? ServerEnv { get; set; }
    public string? LogEventType { get; set; }
    public string? LogEventAction { get; set; }
    public string? Status { get; set; }
    public string? ApplicationName { get; set; }
}
