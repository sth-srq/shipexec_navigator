using System.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ShipExecAgent.DAL;
using ShipExecAgent.DAL.Entities;
using ShipExecAgent.DAL.Managers;
using ShipExecAgent.Shared.Models;

namespace ShipExecAgent.Tests.DAL;

/// <summary>
/// Tests for XmlRepository that exercise its XML-building logic without
/// touching the filesystem.
/// </summary>
public class XmlRepositoryTests
{
    private static XmlRepository CreateSut() =>
        new(NullLogger<XmlRepository>.Instance);

    // ── SerializeAsync / round-trip ───────────────────────────────────────────

    [Fact]
    public async Task SerializeAsync_RootNodeOnly_ProducesValidXml()
    {
        var root = new XmlNodeViewModel { NodeName = "Root" };
        var sut = CreateSut();

        var xml = await sut.SerializeAsync(root);

        // An empty element is serialized as self-closing <Root />
        Assert.Contains("<Root", xml);
    }

    [Fact]
    public async Task SerializeAsync_WithChildValue_ContainsChildElement()
    {
        var root = new XmlNodeViewModel { NodeName = "Config" };
        root.Children.Add(new XmlNodeViewModel
        {
            NodeName = "Version",
            NodeValue = "1.0",
            Depth = 1,
            Parent = root
        });
        var sut = CreateSut();

        var xml = await sut.SerializeAsync(root);

        Assert.Contains("<Version>1.0</Version>", xml);
    }

    [Fact]
    public async Task SerializeAsync_WithAttribute_ContainsAttribute()
    {
        var root = new XmlNodeViewModel { NodeName = "Root" };
        root.Attributes.Add(new XmlAttributeViewModel { Name = "id", Value = "42", OriginalValue = "42" });
        var sut = CreateSut();

        var xml = await sut.SerializeAsync(root);

        Assert.Contains("id=\"42\"", xml);
    }

    [Fact]
    public async Task SerializeAsync_WithNamespacePrefixAttribute_EmitsXmlnsPrefix()
    {
        // Use a prefix namespace (xmlns:xsi) rather than the default namespace
        // to avoid the XmlWriter restriction on redefining the empty prefix.
        var root = new XmlNodeViewModel { NodeName = "Root" };
        root.Attributes.Add(new XmlAttributeViewModel
        {
            Name = "xmlns:xsi",
            Value = "http://www.w3.org/2001/XMLSchema-instance",
            OriginalValue = "http://www.w3.org/2001/XMLSchema-instance",
            IsNamespaceDeclaration = true,
            NamespacePrefix = "xsi"
        });
        var sut = CreateSut();

        var xml = await sut.SerializeAsync(root);

        Assert.Contains("xsi", xml);
    }

    [Fact]
    public async Task SerializeAsync_DeepNesting_ProducesCorrectStructure()
    {
        var root = new XmlNodeViewModel { NodeName = "A" };
        var b = new XmlNodeViewModel { NodeName = "B", Depth = 1, Parent = root };
        b.Children.Add(new XmlNodeViewModel { NodeName = "C", NodeValue = "leaf", Depth = 2, Parent = b });
        root.Children.Add(b);
        var sut = CreateSut();

        var xml = await sut.SerializeAsync(root);

        Assert.Contains("<A>", xml);
        Assert.Contains("<B>", xml);
        Assert.Contains("<C>leaf</C>", xml);
    }

    // ── LoadFromStreamAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task LoadFromStreamAsync_SimpleXml_ReturnsCorrectRootName()
    {
        const string xmlContent = """<?xml version="1.0"?><Configuration><Version>2</Version></Configuration>""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var sut = CreateSut();

        var node = await sut.LoadFromStreamAsync(stream);

        Assert.Equal("Configuration", node.NodeName);
    }

    [Fact]
    public async Task LoadFromStreamAsync_ChildElements_AreMappedToChildren()
    {
        const string xmlContent = """<?xml version="1.0"?><Root><A>1</A><B>2</B></Root>""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var sut = CreateSut();

        var node = await sut.LoadFromStreamAsync(stream);

        Assert.Equal(2, node.Children.Count);
        Assert.Contains(node.Children, c => c.NodeName == "A");
        Assert.Contains(node.Children, c => c.NodeName == "B");
    }

    [Fact]
    public async Task LoadFromStreamAsync_AttributesArePreserved()
    {
        const string xmlContent = """<?xml version="1.0"?><Root id="99" />""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var sut = CreateSut();

        var node = await sut.LoadFromStreamAsync(stream);

        Assert.Contains(node.Attributes, a => a.Name == "id" && a.Value == "99");
    }

    [Fact]
    public async Task LoadFromStreamAsync_RootExpandedForDepthLessThanTwo()
    {
        const string xmlContent = """<?xml version="1.0"?><Root><Child><GrandChild/></Child></Root>""";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var sut = CreateSut();

        var node = await sut.LoadFromStreamAsync(stream);

        Assert.True(node.IsExpanded);                     // depth 0
        Assert.True(node.Children[0].IsExpanded);         // depth 1
        Assert.False(node.Children[0].Children[0].IsExpanded); // depth 2
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SerializeAsync_ThenLoad_RoundTripsNodeValues()
    {
        var root = new XmlNodeViewModel { NodeName = "Company" };
        root.Children.Add(new XmlNodeViewModel { NodeName = "Name", NodeValue = "Acme", Depth = 1, Parent = root });
        var sut = CreateSut();

        var xml = await sut.SerializeAsync(root);
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        var loaded = await sut.LoadFromStreamAsync(ms);

        Assert.Equal("Company", loaded.NodeName);
        var nameNode = loaded.Children.FirstOrDefault(c => c.NodeName == "Name");
        Assert.NotNull(nameNode);
        Assert.Equal("Acme", nameNode.NodeValue);
    }
}

/// <summary>
/// Tests for VarianceManager — connection-level calls are mocked via IDbConnectionFactory.
/// Only logic that can be exercised without a real SQL connection is tested here.
/// </summary>
public class VarianceManagerTests
{
    private static (Mock<IDbConnectionFactory>, Mock<IDbConnection>) CreateMocks()
    {
        var connMock = new Mock<IDbConnection>();
        var factoryMock = new Mock<IDbConnectionFactory>();
        factoryMock.Setup(f => f.CreateConnection()).Returns(connMock.Object);
        return (factoryMock, connMock);
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        var (factoryMock, _) = CreateMocks();
        var ex = Record.Exception(() =>
            new VarianceManager(factoryMock.Object, NullLogger<VarianceManager>.Instance));

        Assert.Null(ex);
    }

    [Fact]
    public void InsertAsync_SetsCreatedOnToUtcNow()
    {
        // We can verify the mutation to the entity before the DB call.
        var (factoryMock, _) = CreateMocks();
        var manager = new VarianceManager(factoryMock.Object, NullLogger<VarianceManager>.Instance);
        var variance = new Variance
        {
            BatchId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            IsActive = false,
            CreatedOn = DateTime.MinValue
        };

        // InsertAsync will throw because the mock connection doesn't execute real SQL,
        // but we only care about the side-effect of setting CreatedOn before the call.
        _ = Record.ExceptionAsync(() => manager.InsertAsync(variance));

        // CreatedOn should have been set to UtcNow before the SQL call was attempted
        Assert.True(variance.CreatedOn > DateTime.MinValue);
        Assert.True(variance.IsActive);
    }
}
