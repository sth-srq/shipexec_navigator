namespace ShipExecNavigator.Shared.Models;

public class SecurityLogEntry
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string? Logger { get; set; }
    public DateTime? LogDate { get; set; }
    public string? Message { get; set; }
    public string? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? TransactionId { get; set; }
    public string? ServerAddress { get; set; }
    public string? Entity { get; set; }
    public string? EntityName { get; set; }
    public string? ActionType { get; set; }
    public string? CurrentValue { get; set; }
    public string? PreviousValue { get; set; }
    public string? EnterpriseId { get; set; }
    public string? SiteId { get; set; }
    public string? ClientAddress { get; set; }
    public string? UserAgentString { get; set; }
    public string? Status { get; set; }
    public string? ApplicationName { get; set; }
}
