using System.Reflection;
using Microsoft.Extensions.Logging;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Services;

public sealed class XmlEnumService : IXmlEnumService
{
    private readonly Dictionary<string, EnumOption[]> _lookup;
    private readonly Dictionary<string, EnumOption[]> _stringLookup;
    private readonly ILogger<XmlEnumService> _logger;

    public XmlEnumService(ILogger<XmlEnumService> logger)
    {
        _logger = logger;
        _logger.LogTrace(">> XmlEnumService constructor — building lookups");
        _lookup = BuildLookup();
        _stringLookup = BuildStringLookup();
        _logger.LogTrace("<< XmlEnumService constructor — {Count} enum entries loaded", _lookup.Count);
    }

    public IReadOnlyList<EnumOption>? GetAllowedValues(string elementName)
    {
        _logger.LogTrace(">> GetAllowedValues({ElementName})", elementName);
        var result = _lookup.TryGetValue(elementName, out var values) ? values : null;
        _logger.LogTrace("<< GetAllowedValues → {Count}", result?.Length ?? 0);
        return result;
    }

    public IReadOnlyList<EnumOption>? GetAllowedValuesAsStrings(string elementName)
    {
        _logger.LogTrace(">> GetAllowedValuesAsStrings({ElementName})", elementName);
        var result = _stringLookup.TryGetValue(elementName, out var values) ? values : null;
        _logger.LogTrace("<< GetAllowedValuesAsStrings → {Count}", result?.Length ?? 0);
        return result;
    }

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
