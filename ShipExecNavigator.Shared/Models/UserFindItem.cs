namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Represents a user identified by the AI chat as matching a find/search query.
/// </summary>
public class UserFindItem
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
