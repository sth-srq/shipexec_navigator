using ShipExecAgent.Shared.Models;

namespace ShipExecAgent.Shared.Interfaces;

public interface IXmlRefLookupService
{
    /// <summary>
    /// Scans the XML tree and builds an entity ID-to-name map per entity type.
    /// Call this after the tree is first loaded and after any structural mutation.
    /// </summary>
    void Rebuild(XmlNodeViewModel root);

    /// <summary>
    /// For a field name ending in "Id" (e.g. "ProfileId"), returns
    /// { Value = numericId, Display = entityName } options found in the tree,
    /// or null if no matching entities exist.
    /// </summary>
    IReadOnlyList<EnumOption>? GetRefOptions(string fieldName);

    /// <summary>
    /// Scans <paramref name="root"/> for GUID-Id + Name entity pairs and merges
    /// them into the existing lookup without clearing company XML data.
    /// Call this after a user XML tree is parsed.
    /// </summary>
    void Extend(XmlNodeViewModel root);

    /// <summary>
    /// Returns the GUID options directly for a named entity type (e.g. "Role", "Permission").
    /// Used when the field being edited is literally named "Id" and parent context
    /// determines which entity type the options belong to.
    /// </summary>
    IReadOnlyList<EnumOption>? GetRefOptionsByEntityType(string entityType);
}
