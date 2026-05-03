using ShipExecAgent.Shared.Models;

namespace ShipExecAgent.Shared.Interfaces;

public interface IXmlEnumService
{
    IReadOnlyList<EnumOption>? GetAllowedValues(string elementName);
    IReadOnlyList<EnumOption>? GetAllowedValuesAsStrings(string elementName);
}
