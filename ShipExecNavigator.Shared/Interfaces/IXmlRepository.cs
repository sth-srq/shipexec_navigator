using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Shared.Interfaces;

public interface IXmlRepository
{
    Task<XmlNodeViewModel> LoadFromFileAsync(string filePath);
    Task<XmlNodeViewModel> LoadFromStreamAsync(Stream stream);
    Task SaveToFileAsync(XmlNodeViewModel root, string filePath);
    Task<string> SerializeAsync(XmlNodeViewModel root);
    Task<string> ExportAsync(XmlNodeViewModel root);
}
