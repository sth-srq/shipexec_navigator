namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Represents a Client Business Rule script update identified by the AI chat.
/// </summary>
public class CbrEditItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>The complete new JavaScript script to replace the existing one.</summary>
    public string Script { get; set; } = string.Empty;
}
