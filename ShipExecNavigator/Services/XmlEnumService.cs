using System.Reflection;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Services;

public sealed class XmlEnumService : IXmlEnumService
{
    private readonly Dictionary<string, EnumOption[]> _lookup;
    private readonly Dictionary<string, EnumOption[]> _stringLookup;

    public XmlEnumService()
    {
        _lookup = BuildLookup();
        _stringLookup = BuildStringLookup();
    }

    public IReadOnlyList<EnumOption>? GetAllowedValues(string elementName)
        => _lookup.TryGetValue(elementName, out var values) ? values : null;

    public IReadOnlyList<EnumOption>? GetAllowedValuesAsStrings(string elementName)
        => _stringLookup.TryGetValue(elementName, out var values) ? values : null;

    private static Dictionary<string, EnumOption[]> BuildLookup()
    {
        var result = new Dictionary<string, EnumOption[]>(StringComparer.OrdinalIgnoreCase);

        var assemblies = new[]
        {
            typeof(PSI.Sox.PrintDirection).Assembly,
        };

        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsEnum) continue;

                IEnumerable<PropertyInfo> props;
                try { props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance); }
                catch { continue; }

                foreach (var prop in props)
                {
                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    if (!propType.IsEnum || result.ContainsKey(prop.Name)) continue;
                    result[prop.Name] = Enum.GetValues(propType)
                        .Cast<object>()
                        .Select(v => new EnumOption { Value = Convert.ToInt32(v).ToString(), Display = v.ToString()! })
                        .ToArray();
                }
            }
        }

        return result;
    }

    private static Dictionary<string, EnumOption[]> BuildStringLookup()
    {
        var result = new Dictionary<string, EnumOption[]>(StringComparer.OrdinalIgnoreCase);
        var assemblies = new[] { typeof(PSI.Sox.PrintDirection).Assembly };
        foreach (var asm in assemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsEnum) continue;
                IEnumerable<PropertyInfo> props;
                try { props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance); }
                catch { continue; }
                foreach (var prop in props)
                {
                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    if (!propType.IsEnum || result.ContainsKey(prop.Name)) continue;
                    result[prop.Name] = Enum.GetValues(propType)
                        .Cast<object>()
                        .Select(v => new EnumOption { Value = v.ToString()!, Display = v.ToString()! })
                        .ToArray();
                }
            }
        }
        return result;
    }
}
