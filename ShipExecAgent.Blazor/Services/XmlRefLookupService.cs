using Microsoft.Extensions.Logging;
using ShipExecAgent.Shared.Interfaces;
using ShipExecAgent.Shared.Models;

namespace ShipExecAgent.Services;

public sealed class XmlRefLookupService(ILogger<XmlRefLookupService> logger) : IXmlRefLookupService
{
    private Dictionary<string, IReadOnlyList<EnumOption>> _lookup =
        new(StringComparer.OrdinalIgnoreCase);

    public void Rebuild(XmlNodeViewModel root)
    {
        logger.LogTrace(">> Rebuild | root={Root}", root.NodeName);
        var dict = new Dictionary<string, List<EnumOption>>(StringComparer.OrdinalIgnoreCase);
        Scan(root, dict);
        _lookup = dict.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<EnumOption>)kvp.Value.AsReadOnly(),
            StringComparer.OrdinalIgnoreCase);
        logger.LogTrace("<< Rebuild → {TypeCount} entity types", _lookup.Count);
    }

    public IReadOnlyList<EnumOption>? GetRefOptions(string fieldName)
    {
        logger.LogTrace(">> GetRefOptions({FieldName})", fieldName);
        if (fieldName.Length <= 2 ||
            !fieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            return null;

        var entityType = fieldName[..^2];
        var result = _lookup.TryGetValue(entityType, out var opts) && opts.Count > 0 ? opts : null;
        logger.LogTrace("<< GetRefOptions → {Count}", result?.Count ?? 0);
        return result;
    }

    public void Extend(XmlNodeViewModel root)
    {
        logger.LogTrace(">> Extend | root={Root}", root.NodeName);
        var dict = new Dictionary<string, List<EnumOption>>(StringComparer.OrdinalIgnoreCase);
        ScanGuids(root, dict);
        foreach (var (key, newList) in dict)
        {
            if (_lookup.TryGetValue(key, out var existing))
            {
                var merged = existing.ToList();
                foreach (var o in newList)
                    if (!merged.Any(e => e.Value == o.Value))
                        merged.Add(o);
                _lookup[key] = merged.AsReadOnly();
            }
            else
            {
                _lookup[key] = newList.AsReadOnly();
            }
        }
    }

    public IReadOnlyList<EnumOption>? GetRefOptionsByEntityType(string entityType)
    {
        logger.LogTrace(">> GetRefOptionsByEntityType({EntityType})", entityType);
        if (string.IsNullOrEmpty(entityType)) return null;
        var result = _lookup.TryGetValue(entityType, out var opts) && opts.Count > 0 ? opts : null;
        logger.LogTrace("<< GetRefOptionsByEntityType → {Count}", result?.Count ?? 0);
        return result;
    }

    private static void ScanGuids(XmlNodeViewModel node, Dictionary<string, List<EnumOption>> dict)
    {
        var idChild = node.Children.FirstOrDefault(c =>
            c.NodeName.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(c.NodeValue, out _));

        var nameChild = node.Children.FirstOrDefault(c =>
            c.NodeName.Equals("Name", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(c.NodeValue));

        if (idChild is not null && nameChild is not null)
        {
            var entityType = node.NodeName;
            if (!dict.TryGetValue(entityType, out var list))
            {
                list = [];
                dict[entityType] = list;
            }
            if (!list.Any(o => o.Value == idChild.NodeValue))
                list.Add(new EnumOption { Value = idChild.NodeValue!, Display = nameChild.NodeValue! });
        }

        foreach (var child in node.Children)
            ScanGuids(child, dict);
    }

    private static void Scan(XmlNodeViewModel node, Dictionary<string, List<EnumOption>> dict)
    {
        // Qualify a node as a named entity when it has both a positive integer Id
        // child and a non-empty Name child.
        var idChild = node.Children.FirstOrDefault(c =>
            c.NodeName.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(c.NodeValue, out var id) && id > 0);

        var nameChild = node.Children.FirstOrDefault(c =>
            c.NodeName.Equals("Name", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(c.NodeValue));

        if (idChild is not null && nameChild is not null)
        {
            var entityType = node.NodeName;
            if (!dict.TryGetValue(entityType, out var list))
            {
                list = [];
                dict[entityType] = list;
            }

            if (!list.Any(o => o.Value == idChild.NodeValue))
                list.Add(new EnumOption
                {
                    Value = idChild.NodeValue!,
                    Display = nameChild.NodeValue!
                });
        }

        foreach (var child in node.Children)
            Scan(child, dict);
    }
}
