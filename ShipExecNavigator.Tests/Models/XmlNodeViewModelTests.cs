using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Tests.Models;

public class XmlNodeViewModelTests
{
    // ── HasChildren ───────────────────────────────────────────────────────────

    [Fact]
    public void HasChildren_NoChildrenNotLazyLoadable_ReturnsFalse()
    {
        var node = new XmlNodeViewModel();

        Assert.False(node.HasChildren);
    }

    [Fact]
    public void HasChildren_WithChildren_ReturnsTrue()
    {
        var node = new XmlNodeViewModel();
        node.Children.Add(new XmlNodeViewModel());

        Assert.True(node.HasChildren);
    }

    [Fact]
    public void HasChildren_LazyLoadableNotYetLoaded_ReturnsTrue()
    {
        var node = new XmlNodeViewModel { IsLazyLoadable = true, IsLazyLoaded = false };

        Assert.True(node.HasChildren);
    }

    [Fact]
    public void HasChildren_LazyLoadableAlreadyLoaded_ReturnsFalse()
    {
        var node = new XmlNodeViewModel { IsLazyLoadable = true, IsLazyLoaded = true };

        Assert.False(node.HasChildren);
    }

    // ── HasValue ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null,    false)]
    [InlineData("",     false)]
    [InlineData("  ",   false)]
    [InlineData("text", true)]
    public void HasValue_VariousInputs(string? value, bool expected)
    {
        var node = new XmlNodeViewModel { NodeValue = value };

        Assert.Equal(expected, node.HasValue);
    }

    // ── DescendantCount ───────────────────────────────────────────────────────

    [Fact]
    public void DescendantCount_NoChildren_IsZero()
    {
        var node = new XmlNodeViewModel();

        Assert.Equal(0, node.DescendantCount);
    }

    [Fact]
    public void DescendantCount_FlatChildren_EqualsChildCount()
    {
        var node = new XmlNodeViewModel();
        node.Children.Add(new XmlNodeViewModel());
        node.Children.Add(new XmlNodeViewModel());

        Assert.Equal(2, node.DescendantCount);
    }

    [Fact]
    public void DescendantCount_NestedChildren_CountsRecursively()
    {
        var root = new XmlNodeViewModel();
        var child = new XmlNodeViewModel();
        child.Children.Add(new XmlNodeViewModel());
        child.Children.Add(new XmlNodeViewModel());
        root.Children.Add(child);

        // child itself (1) + its 2 children = 3
        Assert.Equal(3, root.DescendantCount);
    }

    // ── HasDirectChange ───────────────────────────────────────────────────────

    [Fact]
    public void HasDirectChange_IsModifiedFlag_ReturnsTrue()
    {
        var node = new XmlNodeViewModel { IsModified = true };

        Assert.True(node.HasDirectChange);
    }

    [Fact]
    public void HasDirectChange_ValueMatchesOriginal_ReturnsFalse()
    {
        var node = new XmlNodeViewModel
        {
            NodeValue = "abc",
            OriginalNodeValue = "abc"
        };

        Assert.False(node.HasDirectChange);
    }

    [Fact]
    public void HasDirectChange_ValueDiffersFromOriginal_ReturnsTrue()
    {
        var node = new XmlNodeViewModel
        {
            NodeValue = "new",
            OriginalNodeValue = "old"
        };

        Assert.True(node.HasDirectChange);
    }

    [Fact]
    public void HasDirectChange_AttributeValueChanged_ReturnsTrue()
    {
        var node = new XmlNodeViewModel();
        node.Attributes.Add(new XmlAttributeViewModel
        {
            Name = "id",
            Value = "2",
            OriginalValue = "1"
        });

        Assert.True(node.HasDirectChange);
    }

    [Fact]
    public void HasDirectChange_NamespaceAttributeChanged_ReturnsFalse()
    {
        // Namespace declarations are excluded from change detection
        var node = new XmlNodeViewModel();
        node.Attributes.Add(new XmlAttributeViewModel
        {
            Name = "xmlns",
            Value = "http://new",
            OriginalValue = "http://old",
            IsNamespaceDeclaration = true
        });

        Assert.False(node.HasDirectChange);
    }

    // ── HasAnyChange ──────────────────────────────────────────────────────────

    [Fact]
    public void HasAnyChange_NoChanges_ReturnsFalse()
    {
        var root = new XmlNodeViewModel { NodeValue = "x", OriginalNodeValue = "x" };
        var child = new XmlNodeViewModel { NodeValue = "y", OriginalNodeValue = "y" };
        root.Children.Add(child);

        Assert.False(root.HasAnyChange);
    }

    [Fact]
    public void HasAnyChange_DirectChange_ReturnsTrue()
    {
        var node = new XmlNodeViewModel { IsModified = true };

        Assert.True(node.HasAnyChange);
    }

    [Fact]
    public void HasAnyChange_DescendantChanged_ReturnsTrue()
    {
        var root = new XmlNodeViewModel();
        var child = new XmlNodeViewModel();
        var grandchild = new XmlNodeViewModel { NodeValue = "new", OriginalNodeValue = "old" };
        child.Children.Add(grandchild);
        root.Children.Add(child);

        Assert.True(root.HasAnyChange);
    }

    [Fact]
    public void HasAnyChange_SiblingUnchanged_OnlyChangedNodeReturnsTrue()
    {
        var root = new XmlNodeViewModel();
        var changed = new XmlNodeViewModel { IsModified = true };
        var unchanged = new XmlNodeViewModel { NodeValue = "same", OriginalNodeValue = "same" };
        root.Children.Add(changed);
        root.Children.Add(unchanged);

        Assert.True(root.HasAnyChange);
        Assert.False(unchanged.HasAnyChange);
    }

    // ── DisplayAttributes ─────────────────────────────────────────────────────

    [Fact]
    public void DisplayAttributes_ExcludesNamespaceDeclarations()
    {
        var node = new XmlNodeViewModel();
        node.Attributes.Add(new XmlAttributeViewModel { Name = "xmlns", IsNamespaceDeclaration = true });
        node.Attributes.Add(new XmlAttributeViewModel { Name = "id", Value = "1" });

        var displayed = node.DisplayAttributes.ToList();

        Assert.Single(displayed);
        Assert.Equal("id", displayed[0].Name);
    }

    [Fact]
    public void DisplayAttributes_ExcludesCompanyIdCaseInsensitive()
    {
        var node = new XmlNodeViewModel();
        node.Attributes.Add(new XmlAttributeViewModel { Name = "CompanyId", Value = "abc" });
        node.Attributes.Add(new XmlAttributeViewModel { Name = "companyid", Value = "def" });
        node.Attributes.Add(new XmlAttributeViewModel { Name = "name", Value = "ok" });

        var displayed = node.DisplayAttributes.ToList();

        Assert.Single(displayed);
        Assert.Equal("name", displayed[0].Name);
    }
}
