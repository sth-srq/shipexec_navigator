using ShipExecAgent.Shared.Models;

namespace ShipExecAgent.Tests.Models;

public class XmlAttributeViewModelTests
{
    [Fact]
    public void DefaultValues_AreAllEmptyStrings()
    {
        var attr = new XmlAttributeViewModel();

        Assert.Equal(string.Empty, attr.Name);
        Assert.Equal(string.Empty, attr.Value);
        Assert.Equal(string.Empty, attr.OriginalValue);
        Assert.Equal(string.Empty, attr.NamespacePrefix);
        Assert.False(attr.IsNamespaceDeclaration);
        Assert.False(attr.IsModified);
    }

    [Fact]
    public void ValueAndOriginalValue_CanBeSetIndependently()
    {
        var attr = new XmlAttributeViewModel
        {
            Name = "id",
            Value = "new",
            OriginalValue = "old"
        };

        Assert.Equal("new", attr.Value);
        Assert.Equal("old", attr.OriginalValue);
    }
}

public class CompanyInfoTests
{
    [Fact]
    public void CompanyInfo_DefaultNameAndSymbolEmpty()
    {
        var info = new CompanyInfo();

        Assert.Equal(string.Empty, info.Name);
        Assert.Equal(string.Empty, info.Symbol);
    }

    [Fact]
    public void DisplayName_WithSymbol_IncludesSymbolInParentheses()
    {
        var info = new CompanyInfo { Name = "Acme Corp", Symbol = "ACM" };

        Assert.Equal("Acme Corp (ACM)", info.DisplayName);
    }

    [Fact]
    public void DisplayName_WithoutSymbol_IsNameOnly()
    {
        var info = new CompanyInfo { Name = "Acme Corp", Symbol = string.Empty };

        Assert.Equal("Acme Corp", info.DisplayName);
    }

    [Fact]
    public void DisplayName_WhitespaceOnlySymbol_IsNameOnly()
    {
        var info = new CompanyInfo { Name = "Acme", Symbol = "   " };

        Assert.Equal("Acme", info.DisplayName);
    }
}

public class VarianceInfoTests
{
    [Fact]
    public void VarianceInfo_DefaultsAreEmpty()
    {
        var v = new VarianceInfo();

        Assert.Equal(string.Empty, v.EntityName);
        Assert.Equal(string.Empty, v.ChangeType);
        Assert.Equal(string.Empty, v.ParentContext);
        Assert.Equal(0, v.VarianceIndex);
    }
}
