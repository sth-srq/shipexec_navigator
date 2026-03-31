using System.Reflection;
using System.Xml.Serialization;
using PSI.Sox.Configuration;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Services;

public sealed class XmlSchemaService : IXmlSchemaService
{
    private readonly Dictionary<string, ChildTemplate> _lookup;

    public XmlSchemaService()
    {
        _lookup = BuildLookup();
    }

    public ChildTemplate? GetChildTemplate(string parentElementName)
        => _lookup.TryGetValue(parentElementName, out var t) ? t : null;

    private static Dictionary<string, ChildTemplate> BuildLookup()
    {
        var result = new Dictionary<string, ChildTemplate>(StringComparer.OrdinalIgnoreCase);

        var assemblies = new Assembly[]
        {
            typeof(PSI.Sox.PrintDirection).Assembly,
            typeof(ShipperConfiguration).Assembly,
        };

        List<Type> allTypes;
        try
        {
            allTypes = assemblies
                .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
                .ToList();
        }
        catch
        {
            return result;
        }

        foreach (var type in allTypes)
        {
            if (!type.IsClass || type.IsAbstract) continue;

            PropertyInfo[] props;
            try { props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance); }
            catch { continue; }

            foreach (var prop in props)
            {
                try
                {
                    if (!prop.PropertyType.IsGenericType) continue;
                    if (prop.PropertyType.GetGenericTypeDefinition() != typeof(List<>)) continue;

                    var childType = prop.PropertyType.GetGenericArguments()[0];
                    if (!childType.IsClass) continue;

                    // Already registered — first definition wins
                    if (result.ContainsKey(prop.Name)) continue;

                    // Prefer the XmlArrayItemAttribute element name so we match the actual XML
                    var xmlAttr = prop.GetCustomAttribute<XmlArrayItemAttribute>();
                    var elementName = xmlAttr?.ElementName;
                    if (string.IsNullOrWhiteSpace(elementName))
                        elementName = childType.Name;

                    var fields = BuildFields(childType);
                    if (fields.Count == 0) continue;

                    result[prop.Name] = new ChildTemplate
                    {
                        ElementName = elementName,
                        Fields = fields
                    };
                }
                catch { /* skip any problematic property */ }
            }
        }

        return result;
    }

    private static List<ChildField> BuildFields(Type type)
    {
        var fields = new List<ChildField>();

        PropertyInfo[] props;
        try { props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance); }
        catch { return fields; }

        foreach (var prop in props)
        {
            try
            {
                if (!prop.CanWrite) continue;
                if (ShouldSkip(prop)) continue;

                var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                IReadOnlyList<EnumOption>? allowedValues = null;
                if (underlying.IsEnum)
                    allowedValues = Enum.GetValues(underlying)
                        .Cast<object>()
                        .Select(v => new EnumOption { Value = Convert.ToInt32(v).ToString(), Display = v.ToString()! })
                        .ToArray();
                else if (underlying == typeof(bool))
                    allowedValues = [new EnumOption { Value = "true", Display = "true" }, new EnumOption { Value = "false", Display = "false" }];

                fields.Add(new ChildField { Name = prop.Name, AllowedValues = allowedValues });
            }
            catch { /* skip */ }
        }

        return fields;
    }

    private static bool ShouldSkip(PropertyInfo prop)
    {
        var name = prop.Name;
        var type = prop.PropertyType;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        // Primary key
        if (name.Equals("Id", StringComparison.Ordinal)) return true;

        // Guid identity fields
        if (underlying == typeof(Guid)) return true;

        // Collections
        if (type.IsGenericType) return true;

        // Date/time fields
        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)) return true;

        // Complex class types (navigation properties, etc.)
        if (underlying.IsClass && underlying != typeof(string)) return true;

        // Structs that aren't enums, primitives, or decimal
        if (underlying.IsValueType && !underlying.IsEnum &&
            !underlying.IsPrimitive && underlying != typeof(decimal)) return true;

        return false;
    }
}
