namespace ShipExecAgent.Shared.Models;

/// <summary>
/// Represents a user to be deleted, as identified by the AI chat.
/// </summary>
public class UserDeleteItem
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
