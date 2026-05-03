namespace ShipExecAgent.Shared.Models;

public class UserInfo
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
