using ShipExecAgent.Shared.Models;

namespace ShipExecAgent.Shared.Interfaces;

public interface IXmlSchemaService
{
    /// <summary>
    /// Returns the child element template for the given parent XML element name,
    /// or null if no schema is known for that element.
    /// </summary>
    ChildTemplate? GetChildTemplate(string parentElementName);
}
