using ShipExecNavigator.Shared.Helpers;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Tests.Helpers;

public class CsvUserParserTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Builds a minimal valid CSV string with the given data rows appended.</summary>
    private static string BuildCsv(params string[] dataRows)
    {
        var header = string.Join(",", CsvUserParser.ExpectedHeaders);
        var lines = new List<string> { header };
        lines.AddRange(dataRows);
        return string.Join("\n", lines);
    }

    /// <summary>Returns a data row where every column is empty except Email.</summary>
    private static string MinimalDataRow(string email = "user@example.com")
    {
        // 44 columns expected — fill all with empty, then set Email column (index 13)
        var fields = new string[CsvUserParser.ExpectedHeaders.Length];
        Array.Fill(fields, string.Empty);
        fields[Array.IndexOf(CsvUserParser.ExpectedHeaders, "Email")] = email;
        return string.Join(",", fields);
    }

    // ── Empty / null input ───────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyString_ReturnsSingleErrorRow()
    {
        var rows = CsvUserParser.Parse(string.Empty);

        Assert.Single(rows);
        Assert.Contains("empty", rows[0].Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsSingleErrorRow()
    {
        var rows = CsvUserParser.Parse("   \t  ");

        Assert.Single(rows);
        Assert.False(rows[0].IsValid);
    }

    [Fact]
    public void Parse_HeaderOnlyNoDataRows_ReturnsSingleErrorRow()
    {
        var header = string.Join(",", CsvUserParser.ExpectedHeaders);
        var rows = CsvUserParser.Parse(header);

        Assert.Single(rows);
        Assert.False(rows[0].IsValid);
    }

    // ── Missing / invalid headers ─────────────────────────────────────────────

    [Fact]
    public void Parse_MissingRequiredHeader_ReturnsStructuralError()
    {
        // Drop "Email" from the header
        var badHeaders = CsvUserParser.ExpectedHeaders.Where(h => h != "Email").ToArray();
        var csv = string.Join(",", badHeaders) + "\n" + string.Join(",", new string[badHeaders.Length]);

        var rows = CsvUserParser.Parse(csv);

        Assert.Single(rows);
        Assert.Contains("Email", rows[0].Errors[0]);
    }

    // ── Happy-path parsing ───────────────────────────────────────────────────

    [Fact]
    public void Parse_SingleValidRow_ReturnsOneSuccessRow()
    {
        var csv = BuildCsv(MinimalDataRow("alice@example.com"));

        var rows = CsvUserParser.Parse(csv);

        Assert.Single(rows);
        Assert.True(rows[0].IsValid);
        Assert.Equal("alice@example.com", rows[0].Email);
    }

    [Fact]
    public void Parse_MultipleValidRows_ReturnsCorrectCount()
    {
        var csv = BuildCsv(
            MinimalDataRow("a@b.com"),
            MinimalDataRow("c@d.com"),
            MinimalDataRow("e@f.com"));

        var rows = CsvUserParser.Parse(csv);

        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.True(r.IsValid));
    }

    [Fact]
    public void Parse_RowNumbersAreOneBasedDataRows()
    {
        var csv = BuildCsv(MinimalDataRow(), MinimalDataRow("b@b.com"));

        var rows = CsvUserParser.Parse(csv);

        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal(3, rows[1].RowNumber);
    }

    // ── Field mapping ────────────────────────────────────────────────────────

    [Fact]
    public void Parse_MapsAllIdentityFields()
    {
        var fields = new string[CsvUserParser.ExpectedHeaders.Length];
        Array.Fill(fields, string.Empty);
        fields[Array.IndexOf(CsvUserParser.ExpectedHeaders, "UserID")] = "U001";
        fields[Array.IndexOf(CsvUserParser.ExpectedHeaders, "Email")] = "u@test.com";
        fields[Array.IndexOf(CsvUserParser.ExpectedHeaders, "IdLevel")] = "3";
        fields[Array.IndexOf(CsvUserParser.ExpectedHeaders, "ProfileName")] = "Standard";
        var csv = BuildCsv(string.Join(",", fields));

        var rows = CsvUserParser.Parse(csv);

        var row = Assert.Single(rows);
        Assert.Equal("U001", row.UserId);
        Assert.Equal("u@test.com", row.Email);
        Assert.Equal("3", row.IdLevel);
        Assert.Equal("Standard", row.ProfileName);
    }

    [Fact]
    public void Parse_MapsCanViewFlags_TrueForOne()
    {
        var fields = new string[CsvUserParser.ExpectedHeaders.Length];
        Array.Fill(fields, string.Empty);
        fields[Array.IndexOf(CsvUserParser.ExpectedHeaders, "Email")] = "x@x.com";
        fields[Array.IndexOf(CsvUserParser.ExpectedHeaders, "CanViewHistory")] = "1";
        fields[Array.IndexOf(CsvUserParser.ExpectedHeaders, "CanViewTransmit")] = "true";
        var csv = BuildCsv(string.Join(",", fields));

        var rows = CsvUserParser.Parse(csv);

        var row = Assert.Single(rows);
        Assert.True(row.CanViewHistory);
        Assert.True(row.CanViewTransmit);
        Assert.False(row.CanViewAddressBook);
    }

    // ── Validation ───────────────────────────────────────────────────────────

    [Fact]
    public void Parse_MissingEmail_ProducesValidationError()
    {
        var fields = new string[CsvUserParser.ExpectedHeaders.Length];
        Array.Fill(fields, string.Empty);
        var csv = BuildCsv(string.Join(",", fields));

        var rows = CsvUserParser.Parse(csv);

        Assert.Single(rows);
        Assert.False(rows[0].IsValid);
        Assert.Contains("Email", rows[0].Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_InvalidEmailFormat_ProducesValidationError()
    {
        var csv = BuildCsv(MinimalDataRow("notanemail"));

        var rows = CsvUserParser.Parse(csv);

        Assert.Single(rows);
        Assert.False(rows[0].IsValid);
    }

    [Fact]
    public void Parse_ValidRowHasNoErrors()
    {
        var csv = BuildCsv(MinimalDataRow());

        var rows = CsvUserParser.Parse(csv);

        Assert.Empty(rows[0].Errors);
    }

    // ── CRLF / LF line endings ───────────────────────────────────────────────

    [Fact]
    public void Parse_CrlfLineEndings_ParsedCorrectly()
    {
        var header = string.Join(",", CsvUserParser.ExpectedHeaders);
        var data   = MinimalDataRow("crlf@test.com");
        var csv    = header + "\r\n" + data;

        var rows = CsvUserParser.Parse(csv);

        Assert.Single(rows);
        Assert.Equal("crlf@test.com", rows[0].Email);
    }

    // ── Quoted fields ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("simple", "simple")]
    [InlineData("\"quoted value\"", "quoted value")]
    [InlineData("\"comma,inside\"", "comma,inside")]
    [InlineData("\"double\"\"quote\"", "double\"quote")]
    public void SplitCsvLine_HandlesQuotedFields(string input, string expected)
    {
        var result = CsvUserParser.SplitCsvLine(input);

        Assert.Equal(expected, result[0]);
    }
}
