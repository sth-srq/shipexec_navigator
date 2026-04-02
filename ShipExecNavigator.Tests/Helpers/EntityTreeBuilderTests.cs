using System.Xml.Serialization;
using ShipExecNavigator.Shared.Helpers;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Tests.Helpers;

public class EntityTreeBuilderTests
{
    // ── Test models ───────────────────────────────────────────────────────────

    private class SimpleEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool Active { get; set; }
    }

    private class EntityWithNullable
    {
        public string? OptionalName { get; set; }
        public int RequiredId { get; set; }
    }

    private class EntityWithCollection
    {
        public string Label { get; set; } = "Root";
        public List<SimpleEntity> Items { get; set; } = [];
    }

    private class NestedEntity
    {
        public string Outer { get; set; } = "outer";
        public SimpleEntity Inner { get; set; } = new() { Name = "inner", Count = 5 };
    }

    private class EntityWithXmlElement
    {
        [XmlElement("CustomName")]
        public string RealName { get; set; } = "value";
    }

    private class EntityWithXmlIgnore
    {
        public string Visible { get; set; } = "yes";
        [XmlIgnore]
        public string Hidden { get; set; } = "no";
    }

    // ── FromObject ────────────────────────────────────────────────────────────

    [Fact]
    public void FromObject_SetsNodeNameAndDepth()
    {
        var node = EntityTreeBuilder.FromObject("Root", new SimpleEntity { Name = "x", Count = 1 }, 0, null);

        Assert.Equal("Root", node.NodeName);
        Assert.Equal(0, node.Depth);
        Assert.Null(node.Parent);
    }

    [Fact]
    public void FromObject_ScalarPropertiesBecomeLeavesWithCorrectValues()
    {
        var node = EntityTreeBuilder.FromObject("E", new SimpleEntity { Name = "Alice", Count = 42, Active = true }, 0, null);

        var nameChild  = node.Children.First(c => c.NodeName == "Name");
        var countChild = node.Children.First(c => c.NodeName == "Count");
        var activeChild = node.Children.First(c => c.NodeName == "Active");

        Assert.Equal("Alice", nameChild.NodeValue);
        Assert.Equal("42", countChild.NodeValue);
        Assert.Equal("true", activeChild.NodeValue);
    }

    [Fact]
    public void FromObject_LeafNodesHaveDepthPlusOne()
    {
        var node = EntityTreeBuilder.FromObject("Root", new SimpleEntity { Name = "n", Count = 1 }, 2, null);

        Assert.All(node.Children, c => Assert.Equal(3, c.Depth));
    }

    [Fact]
    public void FromObject_LeafOriginalValueMatchesNodeValue()
    {
        var node = EntityTreeBuilder.FromObject("E", new SimpleEntity { Name = "Test" }, 0, null);

        var nameChild = node.Children.First(c => c.NodeName == "Name");
        Assert.Equal(nameChild.NodeValue, nameChild.OriginalNodeValue);
    }

    [Fact]
    public void FromObject_NullScalarPropertyIsSkipped()
    {
        var entity = new EntityWithNullable { OptionalName = null, RequiredId = 7 };
        var node = EntityTreeBuilder.FromObject("N", entity, 0, null);

        Assert.DoesNotContain(node.Children, c => c.NodeName == "OptionalName");
        Assert.Contains(node.Children, c => c.NodeName == "RequiredId");
    }

    [Fact]
    public void FromObject_CollectionPropertyIsSkipped()
    {
        var entity = new EntityWithCollection { Label = "L" };
        entity.Items.Add(new SimpleEntity { Name = "child" });
        var node = EntityTreeBuilder.FromObject("P", entity, 0, null);

        Assert.DoesNotContain(node.Children, c => c.NodeName == "Items");
        Assert.Contains(node.Children, c => c.NodeName == "Label");
    }

    [Fact]
    public void FromObject_NestedComplexObjectCreatesChildNode()
    {
        var entity = new NestedEntity();
        var node = EntityTreeBuilder.FromObject("N", entity, 0, null);

        var innerNode = node.Children.FirstOrDefault(c => c.NodeName == "Inner");
        Assert.NotNull(innerNode);
        Assert.Equal(1, innerNode.Depth);
        Assert.Contains(innerNode.Children, c => c.NodeName == "Name" && c.NodeValue == "inner");
    }

    [Fact]
    public void FromObject_XmlElementAttributeRenamesChild()
    {
        var entity = new EntityWithXmlElement { RealName = "hello" };
        var node = EntityTreeBuilder.FromObject("Root", entity, 0, null);

        Assert.Contains(node.Children, c => c.NodeName == "CustomName");
        Assert.DoesNotContain(node.Children, c => c.NodeName == "RealName");
    }

    [Fact]
    public void FromObject_XmlIgnoreAttributeExcludesProperty()
    {
        var entity = new EntityWithXmlIgnore();
        var node = EntityTreeBuilder.FromObject("Root", entity, 0, null);

        Assert.Contains(node.Children, c => c.NodeName == "Visible");
        Assert.DoesNotContain(node.Children, c => c.NodeName == "Hidden");
    }

    [Fact]
    public void FromObject_ParentIsSetOnLeafNodes()
    {
        var node = EntityTreeBuilder.FromObject("Root", new SimpleEntity { Name = "x" }, 0, null);

        Assert.All(node.Children, c => Assert.Same(node, c.Parent));
    }

    // ── CreateLazyCategoryNode ────────────────────────────────────────────────

    [Fact]
    public void CreateLazyCategoryNode_SetsAllProperties()
    {
        var parent = new XmlNodeViewModel { NodeName = "Parent" };
        var node = EntityTreeBuilder.CreateLazyCategoryNode("Shippers", "shippers", 2, parent);

        Assert.Equal("Shippers", node.NodeName);
        Assert.Equal("shippers", node.LazyLoadKey);
        Assert.Equal(2, node.Depth);
        Assert.Same(parent, node.Parent);
        Assert.True(node.IsLazyLoadable);
        Assert.False(node.IsLazyLoaded);
    }

    [Fact]
    public void CreateLazyCategoryNode_HasChildrenIsTrue_WhenLazyNotLoaded()
    {
        var parent = new XmlNodeViewModel { NodeName = "P" };
        var node = EntityTreeBuilder.CreateLazyCategoryNode("Items", "items", 1, parent);

        Assert.True(node.HasChildren);
    }

    // ── PopulateCollectionNode ────────────────────────────────────────────────

    [Fact]
    public void PopulateCollectionNode_AddsChildrenForEachItem()
    {
        var parent = new XmlNodeViewModel { NodeName = "Shippers", Depth = 1 };
        var items = new List<SimpleEntity>
        {
            new() { Name = "S1", Count = 1 },
            new() { Name = "S2", Count = 2 },
        };

        EntityTreeBuilder.PopulateCollectionNode(parent, "Shipper", items);

        Assert.Equal(2, parent.Children.Count);
        Assert.All(parent.Children, c => Assert.Equal("Shipper", c.NodeName));
    }

    [Fact]
    public void PopulateCollectionNode_SetsIsLazyLoadedTrue()
    {
        var parent = new XmlNodeViewModel { NodeName = "P", Depth = 0 };
        EntityTreeBuilder.PopulateCollectionNode(parent, "Item", new List<SimpleEntity>());

        Assert.True(parent.IsLazyLoaded);
        Assert.False(parent.IsLoading);
    }

    [Fact]
    public void PopulateCollectionNode_ChildDepthIsParentDepthPlusOne()
    {
        var parent = new XmlNodeViewModel { NodeName = "P", Depth = 3 };
        var items = new List<SimpleEntity> { new() { Name = "x" } };

        EntityTreeBuilder.PopulateCollectionNode(parent, "Item", items);

        Assert.Equal(4, parent.Children[0].Depth);
    }

    [Fact]
    public void PopulateCollectionNode_EmptyCollection_NoChildrenAdded()
    {
        var parent = new XmlNodeViewModel { NodeName = "P", Depth = 0 };
        EntityTreeBuilder.PopulateCollectionNode(parent, "Item", new List<SimpleEntity>());

        Assert.Empty(parent.Children);
        Assert.True(parent.IsLazyLoaded);
    }
}
