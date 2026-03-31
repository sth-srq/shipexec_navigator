using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Shared.Interfaces;

public interface IXmlViewerService
{
    // ── Load ────────────────────────────────────────────────────────────────
    Task<XmlNodeViewModel> LoadFileAsync(string filePath);
    Task<XmlNodeViewModel> LoadStreamAsync(Stream stream, string? fileName = null);

    // ── Tree navigation ─────────────────────────────────────────────────────
    void ExpandAll(XmlNodeViewModel node);
    void CollapseAll(XmlNodeViewModel node);
    void ExpandToDepth(XmlNodeViewModel node, int maxDepth);

    // ── Edit ────────────────────────────────────────────────────────────────
    void AddChild(XmlNodeViewModel parent, string elementName,
                  IEnumerable<(string Name, string Value)> attributes,
                  string? value,
                  IEnumerable<(string ChildName, string ChildValue)> childElements);

    bool RemoveNode(XmlNodeViewModel nodeToRemove);
    void UpdateNodeValue(XmlNodeViewModel node, string newValue);
    void UpdateAttributeValue(XmlNodeViewModel node, string attributeName, string newValue);
    XmlNodeViewModel CloneNode(XmlNodeViewModel source, XmlNodeViewModel? parent = null);

    // ── Clipboard ────────────────────────────────────────────────────────────
    XmlNodeViewModel? Clipboard { get; }
    void SetClipboard(XmlNodeViewModel node);
    void PasteAsChild(XmlNodeViewModel parent);

    // ── Persist ──────────────────────────────────────────────────────────────
    Task SaveToFileAsync(XmlNodeViewModel root, string filePath);
    Task<string> SerializeAsync(XmlNodeViewModel root);
    Task<string> ExportAsync(XmlNodeViewModel root);

    // ── Utilities ────────────────────────────────────────────────────────────
    /// <summary>Finds the node with <paramref name="nodeId"/> in the tree and returns its XML fragment.</summary>
    string SerialiseNodeById(XmlNodeViewModel root, Guid nodeId);
}
