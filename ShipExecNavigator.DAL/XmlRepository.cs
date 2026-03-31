using System.Text;
using System.Xml;
using System.Xml.Linq;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.DAL;

public class XmlRepository : IXmlRepository
{
    public async Task<XmlNodeViewModel> LoadFromFileAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            using var stream = File.OpenRead(filePath);
            var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            return BuildNode(doc.Root!, 0, null);
        });
    }

    public async Task<XmlNodeViewModel> LoadFromStreamAsync(Stream stream)
    {
        var doc = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, CancellationToken.None);
        return BuildNode(doc.Root!, 0, null);
    }

    public async Task SaveToFileAsync(XmlNodeViewModel root, string filePath)
    {
        var xml = await SerializeAsync(root);
        await File.WriteAllTextAsync(filePath, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public async Task<string> SerializeAsync(XmlNodeViewModel root)
    {
        var doc = new XDocument(new XDeclaration("1.0", null, null), BuildXElement(root));
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", Async = true, Encoding = new UTF8Encoding(false) };
        await using var writer = XmlWriter.Create(ms, settings);
        await doc.SaveAsync(writer, CancellationToken.None);
        await writer.FlushAsync();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    // ── Build view model from XML ────────────────────────────────────────────

    private static XmlNodeViewModel BuildNode(XElement element, int depth, XmlNodeViewModel? parent)
    {
        var node = new XmlNodeViewModel
        {
            NodeName = element.Name.LocalName,
            Depth = depth,
            IsExpanded = depth < 2,
            Parent = parent,
            WasEmptyElement = element.IsEmpty
        };

        foreach (var attr in element.Attributes())
        {
            if (attr.IsNamespaceDeclaration)
            {
                node.Attributes.Add(new XmlAttributeViewModel
                {
                    Name = attr.Name.LocalName == "xmlns"
                        ? "xmlns"
                        : $"xmlns:{attr.Name.LocalName}",
                    Value = attr.Value,
                    OriginalValue = attr.Value,
                    IsNamespaceDeclaration = true,
                    NamespacePrefix = attr.Name.LocalName
                });
            }
            else
            {
                node.Attributes.Add(new XmlAttributeViewModel
                {
                    Name = attr.Name.LocalName,
                    Value = attr.Value,
                    OriginalValue = attr.Value
                });
            }
        }

        var childElements = element.Elements().ToList();

        // Display shippers sorted by Id descending so the most recent appear first
        if (element.Name.LocalName == "Shippers" && childElements.Count > 1)
        {
            childElements = childElements
                .OrderByDescending(e => int.TryParse(e.Element("Id")?.Value, out var id) ? id : 0)
                .ToList();
        }

        if (childElements.Count > 0)
        {
            foreach (var child in childElements)
                node.Children.Add(BuildNode(child, depth + 1, node));
        }
        else
        {
            var text = element.Value.Trim();
            node.OriginalNodeValue = element.Value;
            if (!string.IsNullOrWhiteSpace(text))
                node.NodeValue = text;
        }

        return node;
    }

    public async Task<string> ExportAsync(XmlNodeViewModel root)
    {
        var doc = new XDocument(new XDeclaration("1.0", null, null), BuildXElement(root, forExport: true));
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", Async = true, Encoding = new UTF8Encoding(false) };
        await using var writer = XmlWriter.Create(ms, settings);
        await doc.SaveAsync(writer, CancellationToken.None);
        await writer.FlushAsync();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    // ── Build XML from view model ────────────────────────────────────────────

    private static XElement BuildXElement(XmlNodeViewModel node, bool forExport = false)
    {
        var element = new XElement(node.NodeName);

        foreach (var attr in node.Attributes)
        {
            if (attr.IsNamespaceDeclaration)
            {
                element.Add(attr.NamespacePrefix == "xmlns"
                    ? new XAttribute("xmlns", attr.Value)
                    : new XAttribute(XNamespace.Xmlns + attr.NamespacePrefix, attr.Value));
            }
            else
            {
                element.Add(new XAttribute(attr.Name, attr.Value));
            }
        }

        if (node.HasChildren)
        {
            foreach (var child in node.Children)
            {
                if (forExport &&
                    child.NodeName.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
                    child.NodeValue == "-1")
                    continue;

                element.Add(BuildXElement(child, forExport));
            }
        }
        else if (node.HasValue)
        {
            element.Add(new XText(node.NodeValue!));
        }
        else if (forExport && !node.IsModified && node.WasEmptyElement)
        {
            // Preserve original empty element format - keep it self-closing
            // Do nothing, element remains empty
        }
        else if (forExport && !node.IsModified && !string.IsNullOrEmpty(node.OriginalNodeValue))
        {
            // Preserve original format with whitespace
            element.Add(new XText(node.OriginalNodeValue));
        }

        return element;
    }
}
