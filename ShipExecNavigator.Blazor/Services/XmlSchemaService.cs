using System.Reflection;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using PSI.Sox.Configuration;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Services;

/// <summary>
/// Singleton service that builds and caches a schema map of all known child-element
/// templates from the PSI.Sox assemblies via reflection.
/// <para>
/// The schema map powers the context-menu "<b>Add child</b>" feature in the XML tree:
/// when the user right-clicks a collection node (e.g. <c>Shippers</c>), the service
/// looks up the expected child element name and its default field values so the UI can
/// insert a correctly-shaped blank element.
/// </para>
/// <para>
/// <b>Build logic:</b>
/// <list type="number">
///   <item>
///     Iterate over every public class in the PSI.Sox and PSI.Sox.Configuration assemblies.
///   </item>
///   <item>
///     For each <c>List&lt;T&gt;</c> property found, determine the XML element name from
///     <see cref="System.Xml.Serialization.XmlArrayItemAttribute"/> (falling back to the
///     CLR type name) and build a <see cref="ChildTemplate"/> listing all writable
///     scalar properties of <c>T</c>.
///   </item>
///   <item>
///     Index the result by the list property's name (e.g. "Shippers" → ChildTemplate for Shipper).
///   </item>
/// </list>
/// The first definition wins; duplicates are skipped.
/// </para>
/// <para>
/// <b>Registration:</b> <c>Singleton</c> — the reflection cost is paid once at startup.
/// </para>
/// </summary>
public sealed class XmlSchemaService : IXmlSchemaService
{
    private readonly Dictionary<string, ChildTemplate> _lookup;
    private readonly ILogger<XmlSchemaService> _logger;

    public XmlSchemaService(ILogger<XmlSchemaService> logger)
    {
        _logger = logger;
        _logger.LogTrace(">> XmlSchemaService constructor — building lookup");
        _lookup = BuildLookup();
        _logger.LogTrace("<< XmlSchemaService constructor — {Count} schema entries loaded", _lookup.Count);
    }

    public ChildTemplate? GetChildTemplate(string parentElementName)
    {
        _logger.LogTrace(">> GetChildTemplate({Parent})", parentElementName);
        var result = _lookup.TryGetValue(parentElementName, out var t) ? t : null;
        _logger.LogTrace("<< GetChildTemplate → {Found}", result is not null ? result.ElementName : "null");
        return result;
    }

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
