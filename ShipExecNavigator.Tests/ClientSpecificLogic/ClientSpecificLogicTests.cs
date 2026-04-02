using PSI.Sox;
using ShipExecNavigator.ClientSpecificLogic;

namespace ShipExecNavigator.Tests.ClientSpecificLogic;

public class ClientLogicResolverTests
{
    [Fact]
    public void Resolve_NullCompanyName_ReturnsDefaultCompanyLogic()
    {
        var result = ClientLogicResolver.Resolve(null);

        Assert.IsType<DefaultCompanyLogic>(result);
    }

    [Fact]
    public void Resolve_EmptyCompanyName_ReturnsDefaultCompanyLogic()
    {
        var result = ClientLogicResolver.Resolve(string.Empty);

        Assert.IsType<DefaultCompanyLogic>(result);
    }

    [Fact]
    public void Resolve_UnknownCompanyName_ReturnsDefaultCompanyLogic()
    {
        var result = ClientLogicResolver.Resolve("Acme Corp");

        Assert.IsType<DefaultCompanyLogic>(result);
    }

    [Fact]
    public void Resolve_WesbancoExactCase_ReturnsWesbancoLogic()
    {
        var result = ClientLogicResolver.Resolve("WesBanco");

        Assert.IsType<WesbancoClientSpecificLogic>(result);
    }

    [Fact]
    public void Resolve_WesbancoLowerCase_ReturnsWesbancoLogic()
    {
        var result = ClientLogicResolver.Resolve("wesbanco bank");

        Assert.IsType<WesbancoClientSpecificLogic>(result);
    }

    [Fact]
    public void Resolve_WesbancoUpperCase_ReturnsWesbancoLogic()
    {
        var result = ClientLogicResolver.Resolve("WESBANCO");

        Assert.IsType<WesbancoClientSpecificLogic>(result);
    }

    [Fact]
    public void Resolve_WesbancoSubstring_ReturnsWesbancoLogic()
    {
        var result = ClientLogicResolver.Resolve("WesbancoBank NA");

        Assert.IsType<WesbancoClientSpecificLogic>(result);
    }
}

public class DefaultCompanyLogicTests
{
    private static Shipper MakeShipper(int id = 0, string symbol = "", string name = "") =>
        new() { Id = id, Symbol = symbol, Name = name };

    private readonly DefaultCompanyLogic _sut = new();

    // ── FindMatchingShipper ───────────────────────────────────────────────────

    [Fact]
    public void FindMatchingShipper_MatchById_ReturnsMatch()
    {
        var existing = new List<Shipper>
        {
            MakeShipper(id: 10, symbol: "A"),
            MakeShipper(id: 20, symbol: "B"),
        };
        var incoming = MakeShipper(id: 20);

        var result = _sut.FindMatchingShipper(existing, incoming);

        Assert.NotNull(result);
        Assert.Equal("B", result.Symbol);
    }

    [Fact]
    public void FindMatchingShipper_MatchBySymbolCaseInsensitive_ReturnsMatch()
    {
        var existing = new List<Shipper> { MakeShipper(symbol: "UPS") };
        var incoming = MakeShipper(id: 0, symbol: "ups");

        var result = _sut.FindMatchingShipper(existing, incoming);

        Assert.NotNull(result);
        Assert.Equal("UPS", result.Symbol);
    }

    [Fact]
    public void FindMatchingShipper_NoMatch_ReturnsNull()
    {
        var existing = new List<Shipper> { MakeShipper(id: 1, symbol: "FedEx") };
        var incoming = MakeShipper(id: 99, symbol: "DHL");

        var result = _sut.FindMatchingShipper(existing, incoming);

        Assert.Null(result);
    }

    [Fact]
    public void FindMatchingShipper_EmptyExistingList_ReturnsNull()
    {
        var result = _sut.FindMatchingShipper([], MakeShipper(id: 1, symbol: "X"));

        Assert.Null(result);
    }

    [Fact]
    public void FindMatchingShipper_IdTakesPrecedenceOverSymbol()
    {
        var existing = new List<Shipper>
        {
            MakeShipper(id: 5, symbol: "BySymbol"),
            MakeShipper(id: 10, symbol: "ById"),
        };
        // incoming has id=10 but symbol matches id=5
        var incoming = MakeShipper(id: 10, symbol: "BySymbol");

        var result = _sut.FindMatchingShipper(existing, incoming);

        Assert.Equal("ById", result?.Symbol);
    }

    // ── GetShipperExportExtraHeaders ──────────────────────────────────────────

    [Fact]
    public void GetShipperExportExtraHeaders_ReturnsEmpty()
    {
        var headers = _sut.GetShipperExportExtraHeaders();

        Assert.Empty(headers);
    }

    // ── GetShipperExportExtraValues ───────────────────────────────────────────

    [Fact]
    public void GetShipperExportExtraValues_ReturnsEmpty()
    {
        var values = _sut.GetShipperExportExtraValues(MakeShipper(symbol: "X"));

        Assert.Empty(values);
    }
}

public class WesbancoClientSpecificLogicTests
{
    private static Shipper MakeShipper(string name) => new() { Name = name };

    private readonly WesbancoClientSpecificLogic _sut = new();

    // ── FindMatchingShipper ───────────────────────────────────────────────────

    [Fact]
    public void FindMatchingShipper_MatchesByKeyInParentheses()
    {
        var existing = new List<Shipper>
        {
            MakeShipper("Wesbanco (WB-001)"),
            MakeShipper("Other (OTH)"),
        };
        var incoming = MakeShipper("New Shipper (WB-001)");

        var result = _sut.FindMatchingShipper(existing, incoming);

        Assert.NotNull(result);
        Assert.Equal("Wesbanco (WB-001)", result.Name);
    }

    [Fact]
    public void FindMatchingShipper_NoParentheses_ReturnsNull()
    {
        var existing = new List<Shipper> { MakeShipper("Wesbanco") };
        var incoming = MakeShipper("No parens shipper");

        var result = _sut.FindMatchingShipper(existing, incoming);

        Assert.Null(result);
    }

    [Fact]
    public void FindMatchingShipper_EmptyIncomingName_ReturnsNull()
    {
        var existing = new List<Shipper> { MakeShipper("Some (X)") };

        var result = _sut.FindMatchingShipper(existing, MakeShipper(string.Empty));

        Assert.Null(result);
    }

    [Fact]
    public void FindMatchingShipper_KeyNotFoundInExisting_ReturnsNull()
    {
        var existing = new List<Shipper> { MakeShipper("Bank (OTH)") };
        var incoming = MakeShipper("Test (XYZ)");

        var result = _sut.FindMatchingShipper(existing, incoming);

        Assert.Null(result);
    }

    [Fact]
    public void FindMatchingShipper_CaseInsensitiveKeyMatch()
    {
        var existing = new List<Shipper> { MakeShipper("Wesbanco (WB-001)") };
        var incoming = MakeShipper("Shipper (wb-001)");

        var result = _sut.FindMatchingShipper(existing, incoming);

        Assert.NotNull(result);
    }

    // ── GetShipperExportExtraHeaders ──────────────────────────────────────────

    [Fact]
    public void GetShipperExportExtraHeaders_ReturnsBankId()
    {
        var headers = _sut.GetShipperExportExtraHeaders();

        Assert.Single(headers);
        Assert.Equal("BankId", headers[0]);
    }

    // ── GetShipperExportExtraValues ───────────────────────────────────────────

    [Fact]
    public void GetShipperExportExtraValues_ExtractsKeyFromParentheses()
    {
        var shipper = MakeShipper("Wesbanco Bank (WB-999)");

        var values = _sut.GetShipperExportExtraValues(shipper);

        Assert.Single(values);
        Assert.Equal("WB-999", values[0]);
    }

    [Fact]
    public void GetShipperExportExtraValues_NoParentheses_ReturnsEmptyString()
    {
        var shipper = MakeShipper("Wesbanco Bank");

        var values = _sut.GetShipperExportExtraValues(shipper);

        Assert.Single(values);
        Assert.Equal(string.Empty, values[0]);
    }

    [Fact]
    public void GetShipperExportExtraValues_EmptyName_ReturnsEmptyString()
    {
        var values = _sut.GetShipperExportExtraValues(MakeShipper(string.Empty));

        Assert.Single(values);
        Assert.Equal(string.Empty, values[0]);
    }

    [Fact]
    public void GetShipperExportExtraValues_HeaderAndValueCountMatch()
    {
        var shipper = MakeShipper("Bank (KEY)");

        var headers = _sut.GetShipperExportExtraHeaders();
        var values  = _sut.GetShipperExportExtraValues(shipper);

        Assert.Equal(headers.Count, values.Count);
    }
}
