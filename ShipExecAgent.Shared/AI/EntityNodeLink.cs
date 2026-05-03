namespace ShipExecAgent.Shared.AI;

/// <summary>
/// Describes a clickable link embedded in an AI chat message that navigates
/// to an entity or category node in the Navigator tree.
/// </summary>
public class EntityNodeLink
{
    /// <summary>
    /// The plural category name matching the tree node (e.g. "Shippers", "Profiles").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Optional entity name within the category.  When set, the link targets a
    /// specific entity node; when <see langword="null"/>, it targets the category root.
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// Display label shown as clickable text in the chat bubble.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
