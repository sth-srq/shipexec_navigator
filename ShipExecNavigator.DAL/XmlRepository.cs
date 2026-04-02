using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ShipExecNavigator.Shared.Interfaces;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.DAL;

/// <summary>
/// Reads and writes ShipExec company configuration XML files, converting between
/// the on-disk XML format and the <see cref="XmlNodeViewModel"/> tree used by
/// the Blazor Navigator UI.
/// <para>
/// <b>Load path:</b> <see cref="LoadFromFileAsync"/> / <see cref="LoadFromStreamAsync"/>
/// parse the XML using LINQ-to-XML (<see cref="System.Xml.Linq.XDocument"/>) with
/// whitespace preserved so round-tripping does not alter the document.
/// Each <see cref="System.Xml.Linq.XElement"/> is mapped to an
/// <see cref="XmlNodeViewModel"/> whose children are recursively built.
/// Nodes at depth 0 and 1 are auto-expanded; deeper nodes start collapsed.
/// </para>
/// <para>
/// <b>Special ordering:</b> the <c>Shippers</c> collection is sorted by descending ID
/// so the most recently added shippers appear at the top of the tree.
/// </para>
/// <para>
/// <b>Save path:</b> <see cref="SerializeAsync"/> walks the
/// <see cref="XmlNodeViewModel"/> tree in reverse and reconstructs an
/// <see cref="System.Xml.Linq.XDocument"/>, then writes it with two-space indentation
/// and a UTF-8 BOM-free encoding to match the format produced by the
/// <see cref="CompanyExportRequestGenerator"/>.
/// </para>
/// </summary>
public class XmlRepository(ILogger<XmlRepository> logger) : IXmlRepository
{
    public async Task<XmlNodeViewModel> LoadFromFileAsync(string filePath)
    {
        logger.LogTrace(">> LoadFromFileAsync({FilePath})", filePath);
        var result = await Task.Run(() =>
        {
            using var stream = File.OpenRead(filePath);
            var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            return BuildNode(doc.Root!, 0, null);
        });
        logger.LogTrace("<< LoadFromFileAsync → root={Root}", result.NodeName);
        return result;
    }

    public async Task<XmlNodeViewModel> LoadFromStreamAsync(Stream stream)
    {
        logger.LogTrace(">> LoadFromStreamAsync");
        var doc = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, CancellationToken.None);
        var result = BuildNode(doc.Root!, 0, null);
        logger.LogTrace("<< LoadFromStreamAsync → root={Root}", result.NodeName);
        return result;
    }

    public async Task SaveToFileAsync(XmlNodeViewModel root, string filePath)
    {
        logger.LogTrace(">> SaveToFileAsync({FilePath})", filePath);
        var xml = await SerializeAsync(root);
        await File.WriteAllTextAsync(filePath, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        logger.LogTrace("<< SaveToFileAsync");
    }

    public async Task<string> SerializeAsync(XmlNodeViewModel root)
    {
        logger.LogTrace(">> SerializeAsync | root={Root}", root.NodeName);
        var doc = new XDocument(new XDeclaration("1.0", null, null), BuildXElement(root));
        using var ms = new MemoryStream();
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", Async = true, Encoding = new UTF8Encoding(false) };
        await using var writer = XmlWriter.Create(ms, settings);
        await doc.SaveAsync(writer, CancellationToken.None);
        await writer.FlushAsync();
        var xml = Encoding.UTF8.GetString(ms.ToArray());
        logger.LogTrace("<< SerializeAsync → {Length} chars", xml.Length);
        return xml;
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
