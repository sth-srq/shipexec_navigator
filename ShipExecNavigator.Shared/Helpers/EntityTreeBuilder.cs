using System.Collections;
using System.Reflection;
using System.Xml.Serialization;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Shared.Helpers;

/// <summary>
/// Converts entity objects (e.g., PSI.Sox.Shipper) into <see cref="XmlNodeViewModel"/> trees
/// using reflection, matching the same property names that XmlSerializer would produce.
/// </summary>
public static class EntityTreeBuilder
{
    private static readonly HashSet<Type> ScalarTypes =
    [
        typeof(string), typeof(int), typeof(long), typeof(short), typeof(byte),
        typeof(float), typeof(double), typeof(decimal), typeof(bool),
        typeof(Guid), typeof(DateTime), typeof(DateTimeOffset),
    ];

    /// <summary>
    /// Builds an <see cref="XmlNodeViewModel"/> tree from a single entity object.
    /// Scalar properties become leaf child nodes; complex objects recurse.
    /// Collections are skipped (they should be handled as lazy-loadable category nodes).
    /// </summary>
    public static XmlNodeViewModel FromObject(string nodeName, object obj, int depth, XmlNodeViewModel? parent)
    {
        var node = new XmlNodeViewModel
        {
            NodeName = nodeName,
            Depth = depth,
            Parent = parent,
            IsExpanded = false,
        };

        foreach (var prop in GetSerializableProperties(obj.GetType()))
        {
            var propType = prop.PropertyType;
            var underlying = Nullable.GetUnderlyingType(propType) ?? propType;

            if (IsScalar(underlying))
            {
                var value = prop.GetValue(obj);
                if (value is null) continue;

                var strValue = FormatValue(value, underlying);
                var child = new XmlNodeViewModel
                {
                    NodeName = GetElementName(prop),
                    NodeValue = strValue,
                    OriginalNodeValue = strValue,
                    Depth = depth + 1,
                    Parent = node,
                };
                node.Children.Add(child);
            }
            else if (IsCollection(propType))
            {
                // Skip collections — they're handled as lazy-loadable category nodes
                continue;
            }
            else if (underlying.IsClass && !underlying.IsAbstract)
            {
                var value = prop.GetValue(obj);
                if (value is null) continue;

                var childNode = FromObject(GetElementName(prop), value, depth + 1, node);
                node.Children.Add(childNode);
            }
        }

        return node;
    }

    /// <summary>
    /// Builds child nodes for a collection of entities and adds them to the parent node.
    /// </summary>
    public static void PopulateCollectionNode(
        XmlNodeViewModel parentNode,
        string itemElementName,
        IEnumerable items)
    {
        var depth = parentNode.Depth + 1;
        foreach (var item in items)
        {
            var childNode = FromObject(itemElementName, item, depth, parentNode);
            parentNode.Children.Add(childNode);
        }
        parentNode.IsLazyLoaded = true;
        parentNode.IsLoading = false;
    }

    /// <summary>
    /// Creates a lazy-loadable category node (e.g., "Shippers") with no children loaded yet.
    /// </summary>
    public static XmlNodeViewModel CreateLazyCategoryNode(
        string categoryName, string lazyLoadKey, int depth, XmlNodeViewModel parent)
    {
        return new XmlNodeViewModel
        {
            NodeName = categoryName,
            Depth = depth,
            Parent = parent,
            IsLazyLoadable = true,
            IsLazyLoaded = false,
            LazyLoadKey = lazyLoadKey,
        };
    }

    private static string FormatValue(object value, Type underlying)
    {
        if (underlying == typeof(bool))
            return (bool)value ? "true" : "false";
        if (underlying.IsEnum)
            return Convert.ToInt32(value).ToString();
        return value.ToString() ?? string.Empty;
    }

    private static bool IsScalar(Type type) =>
        type.IsPrimitive || ScalarTypes.Contains(type) || type.IsEnum;

    private static bool IsCollection(Type type) =>
        type != typeof(string) &&
        (typeof(IEnumerable).IsAssignableFrom(type) ||
         (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)));

    private static string GetElementName(PropertyInfo prop)
    {
        var xmlElem = prop.GetCustomAttribute<XmlElementAttribute>();
        if (xmlElem is not null && !string.IsNullOrEmpty(xmlElem.ElementName))
            return xmlElem.ElementName;
        return prop.Name;
    }

    private static IEnumerable<PropertyInfo> GetSerializableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Where(p => p.GetCustomAttribute<XmlIgnoreAttribute>() is null)
            .OrderBy(p => p.MetadataToken);
    }
}
