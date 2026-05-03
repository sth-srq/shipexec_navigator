namespace ShipExecAgent.Shared.Models;

public class XmlNodeViewModel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string NodeName { get; set; } = string.Empty;
    public string? NodeValue { get; set; }
    public List<XmlAttributeViewModel> Attributes { get; set; } = [];
    public List<XmlNodeViewModel> Children { get; set; } = [];
    public bool IsExpanded { get; set; }
    public int Depth { get; set; }
    public XmlNodeViewModel? Parent { get; set; }

    // Track original state for export formatting
    public string? OriginalNodeValue { get; set; }
    public bool WasEmptyElement { get; set; }
    public bool IsModified { get; set; }

    // Lazy loading support
    public bool IsLazyLoadable { get; set; }
    public bool IsLazyLoaded { get; set; }
    public bool IsLoading { get; set; }
    public string? LazyLoadKey { get; set; }

    public bool HasChildren => Children.Count > 0 || (IsLazyLoadable && !IsLazyLoaded);
    public bool HasValue => !string.IsNullOrWhiteSpace(NodeValue);

    public IEnumerable<XmlAttributeViewModel> DisplayAttributes
        => Attributes.Where(a => !a.IsNamespaceDeclaration
                               && !a.Name.Equals("CompanyId", StringComparison.OrdinalIgnoreCase));

    public int DescendantCount => Children.Sum(c => 1 + c.DescendantCount);

    /// <summary>True when this node's own value, attributes, or IsModified flag indicate a change.</summary>
    public bool HasDirectChange =>
        IsModified
        || (OriginalNodeValue is not null && OriginalNodeValue.Trim() != (NodeValue ?? string.Empty))
        || Attributes.Any(a => !a.IsNamespaceDeclaration && a.Value != a.OriginalValue);

    /// <summary>True when this node or any descendant has a change.</summary>
    public bool HasAnyChange => HasDirectChange || Children.Any(c => c.HasAnyChange);
}