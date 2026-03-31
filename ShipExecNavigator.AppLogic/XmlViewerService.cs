using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.AppLogic;

public class XmlViewerService : IXmlViewerService
{
    private readonly IXmlRepository _repository;
    private XmlNodeViewModel? _clipboard;

    public XmlViewerService(IXmlRepository repository) => _repository = repository;

    // ── Load ─────────────────────────────────────────────────────────────────

    public Task<XmlNodeViewModel> LoadFileAsync(string filePath)
        => _repository.LoadFromFileAsync(filePath);

    public Task<XmlNodeViewModel> LoadStreamAsync(Stream stream, string? fileName = null)
        => _repository.LoadFromStreamAsync(stream);

    // ── Tree navigation ───────────────────────────────────────────────────────

    public void ExpandAll(XmlNodeViewModel node)
    {
        node.IsExpanded = true;
        foreach (var child in node.Children) ExpandAll(child);
    }

    public void CollapseAll(XmlNodeViewModel node)
    {
        node.IsExpanded = false;
        foreach (var child in node.Children) CollapseAll(child);
    }

    public void ExpandToDepth(XmlNodeViewModel node, int maxDepth)
    {
        node.IsExpanded = node.Depth < maxDepth;
        foreach (var child in node.Children) ExpandToDepth(child, maxDepth);
    }

    // ── Edit operations ───────────────────────────────────────────────────────

    public void AddChild(XmlNodeViewModel parent, string elementName,
                         IEnumerable<(string Name, string Value)> attributes,
                         string? value,
                         IEnumerable<(string ChildName, string ChildValue)> childElements)
    {
        var depth = parent.Depth + 1;
        var newNode = new XmlNodeViewModel
        {
            NodeName = elementName,
            Depth = depth,
            Parent = parent,
            IsExpanded = true,
            IsModified = true // New nodes are considered modified
        };

        foreach (var (name, val) in attributes)
            newNode.Attributes.Add(new XmlAttributeViewModel
            {
                Name = name,
                Value = val,
                OriginalValue = val
            });

        var kids = childElements.ToList();
        if (kids.Count > 0)
        {
            foreach (var (childName, childValue) in kids)
            {
                newNode.Children.Add(new XmlNodeViewModel
                {
                    NodeName = childName,
                    NodeValue = string.IsNullOrWhiteSpace(childValue) ? null : childValue,
                    Depth = depth + 1,
                    Parent = newNode,
                    IsModified = true // New child nodes are modified
                });
            }
        }
        else if (!string.IsNullOrWhiteSpace(value))
        {
            newNode.NodeValue = value;
        }

        parent.NodeValue = null;
        parent.IsExpanded = true;
        parent.IsModified = true; // Parent is modified when adding children
        parent.Children.Add(newNode);
    }

    public bool RemoveNode(XmlNodeViewModel nodeToRemove)
    {
        var parent = nodeToRemove.Parent;
        if (parent is null) return false;
        return parent.Children.Remove(nodeToRemove);
    }

    public void UpdateNodeValue(XmlNodeViewModel node, string newValue)
    {
        var trimmedValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim();
        node.NodeValue = trimmedValue;
        node.IsModified = true;
    }

    public void UpdateAttributeValue(XmlNodeViewModel node, string attributeName, string newValue)
    {
        var attr = node.Attributes.FirstOrDefault(a => a.Name == attributeName);
        if (attr is not null)
        {
            attr.Value = newValue;
            attr.IsModified = true;
        }
    }

    public XmlNodeViewModel CloneNode(XmlNodeViewModel source, XmlNodeViewModel? parent = null)
    {
        var depth = parent is not null ? parent.Depth + 1 : source.Depth;
        return DeepClone(source, parent, depth);
    }

    private static XmlNodeViewModel DeepClone(XmlNodeViewModel src, XmlNodeViewModel? parent, int depth)
    {
        var clone = new XmlNodeViewModel
        {
            NodeName = src.NodeName,
            NodeValue = src.NodeValue,
            IsExpanded = src.IsExpanded,
            Depth = depth,
            Parent = parent,
            OriginalNodeValue = src.OriginalNodeValue,
            WasEmptyElement = src.WasEmptyElement,
            IsModified = src.IsModified,
            Attributes = src.Attributes.Select(a => new XmlAttributeViewModel
            {
                Name = a.Name, 
                Value = a.Value,
                OriginalValue = a.OriginalValue,
                IsModified = a.IsModified,
                IsNamespaceDeclaration = a.IsNamespaceDeclaration,
                NamespacePrefix = a.NamespacePrefix
            }).ToList()
        };

        foreach (var child in src.Children)
            clone.Children.Add(DeepClone(child, clone, depth + 1));

        return clone;
    }

    // ── Clipboard ─────────────────────────────────────────────────────────────

    public XmlNodeViewModel? Clipboard => _clipboard;

    public void SetClipboard(XmlNodeViewModel node)
    {
        _clipboard = DeepClone(node, null, 0);
    }

    public void PasteAsChild(XmlNodeViewModel parent)
    {
        if (_clipboard is null) return;
        var pasted = DeepClone(_clipboard, parent, parent.Depth + 1);
        SetDepths(pasted, parent.Depth + 1);
        parent.NodeValue = null;
        parent.IsExpanded = true;
        parent.Children.Add(pasted);
    }

    private static void SetDepths(XmlNodeViewModel node, int depth)
    {
        node.Depth = depth;
        foreach (var child in node.Children) SetDepths(child, depth + 1);
    }

    // ── Persist ───────────────────────────────────────────────────────────────

    public Task SaveToFileAsync(XmlNodeViewModel root, string filePath)
        => _repository.SaveToFileAsync(root, filePath);

    public Task<string> SerializeAsync(XmlNodeViewModel root)
        => _repository.SerializeAsync(root);

    public Task<string> ExportAsync(XmlNodeViewModel root)
        => _repository.ExportAsync(root);

    public string SerialiseNodeById(XmlNodeViewModel root, Guid nodeId)
    {
        var node = FindById(root, nodeId);
        if (node is null) return string.Empty;
        return _repository.SerializeAsync(node).GetAwaiter().GetResult();
    }

    private static XmlNodeViewModel? FindById(XmlNodeViewModel node, Guid id)
    {
        if (node.Id == id) return node;
        foreach (var child in node.Children)
        {
            var found = FindById(child, id);
            if (found is not null) return found;
        }
        return null;
    }
}

