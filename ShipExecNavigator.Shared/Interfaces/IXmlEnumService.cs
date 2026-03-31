using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Shared.Interfaces;

public interface IXmlEnumService
{
    IReadOnlyList<EnumOption>? GetAllowedValues(string elementName);
    IReadOnlyList<EnumOption>? GetAllowedValuesAsStrings(string elementName);
}
