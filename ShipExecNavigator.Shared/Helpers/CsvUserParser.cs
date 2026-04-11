using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.Shared.Helpers;

public static class CsvUserParser
{
    public static readonly string[] ExpectedHeaders =
    [
        "UserID", "Campus", "Campus Displayname", "Company",
        "FName", "LName", "Addr1", "Addr2", "Addr3",
        "City", "State", "Zip", "Country", "Email", "Phone",
        "IdLevel",
        "Custom1", "Custom2", "Custom3", "Custom4", "Custom5",
        "Custom6", "Custom7", "Custom8", "Custom9", "Custom10",
        "CanViewAddressBook", "CanViewBatchManager", "CanViewCloseout",
        "CanViewCreateBatch", "CanViewDistributionList", "CanViewGroupManager",
        "CanViewHistory", "CanViewManageData", "CanViewManifestDocuments",
        "CanViewPickupRequest", "CanViewScanAndShip", "CanViewShippingAndRating",
        "CanViewTransmit",
        "ExportFileDelimiter", "ExportFileQualifier",
        "ExportFileGroupSeparator", "ExportFileDecimalSeparator",
        "ProfileName"
    ];

    /// <summary>
    /// Parses CSV content into CsvUserRow objects with per-row validation errors.
    /// Returns a single structural error row if the file header is invalid.
    /// </summary>
    public static List<CsvUserRow> Parse(string csvContent)
    {
        var rows = new List<CsvUserRow>();

        if (string.IsNullOrWhiteSpace(csvContent))
        {
            rows.Add(new CsvUserRow { RowNumber = 0, Errors = ["The file is empty."] });
            return rows;
        }

        var lines = csvContent
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            rows.Add(new CsvUserRow { RowNumber = 0, Errors = ["The file must contain a header row and at least one data row."] });
            return rows;
        }

        var headers = SplitCsvLine(lines[0]);
        var headerIndex = BuildHeaderIndex(headers);

        var missingHeaders = ExpectedHeaders
            .Where(h => !headerIndex.ContainsKey(h))
            .ToList();

        if (missingHeaders.Count > 0)
        {
            rows.Add(new CsvUserRow
            {
                RowNumber = 0,
                Errors = [$"Missing required columns: {string.Join(", ", missingHeaders)}"]
            });
            return rows;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var fields = SplitCsvLine(line);
            var row = new CsvUserRow { RowNumber = i + 1 };

            // Identity / account
            row.UserId = GetField(fields, headerIndex, "UserID");
            row.Email = GetField(fields, headerIndex, "Email");
            row.IdLevel = GetField(fields, headerIndex, "IdLevel");
            row.ProfileName = GetField(fields, headerIndex, "ProfileName");

            // Campus / site
            row.Campus = GetField(fields, headerIndex, "Campus");
            row.CampusDisplayName = GetField(fields, headerIndex, "Campus Displayname");

            // Name / address
            row.Company = GetField(fields, headerIndex, "Company");
            row.FName = GetField(fields, headerIndex, "FName");
            row.LName = GetField(fields, headerIndex, "LName");
            row.Addr1 = GetField(fields, headerIndex, "Addr1");
            row.Addr2 = GetField(fields, headerIndex, "Addr2");
            row.Addr3 = GetField(fields, headerIndex, "Addr3");
            row.City = GetField(fields, headerIndex, "City");
            row.State = GetField(fields, headerIndex, "State");
            row.Zip = GetField(fields, headerIndex, "Zip");
            row.Country = GetField(fields, headerIndex, "Country");
            row.Phone = GetField(fields, headerIndex, "Phone");

            // Custom attributes
            row.Custom1 = GetField(fields, headerIndex, "Custom1");
            row.Custom2 = GetField(fields, headerIndex, "Custom2");
            row.Custom3 = GetField(fields, headerIndex, "Custom3");
            row.Custom4 = GetField(fields, headerIndex, "Custom4");
            row.Custom5 = GetField(fields, headerIndex, "Custom5");
            row.Custom6 = GetField(fields, headerIndex, "Custom6");
            row.Custom7 = GetField(fields, headerIndex, "Custom7");
            row.Custom8 = GetField(fields, headerIndex, "Custom8");
            row.Custom9 = GetField(fields, headerIndex, "Custom9");
            row.Custom10 = GetField(fields, headerIndex, "Custom10");

            // CanView* permission flags
            row.CanViewAddressBook = ParseBool(GetField(fields, headerIndex, "CanViewAddressBook"));
            row.CanViewBatchManager = ParseBool(GetField(fields, headerIndex, "CanViewBatchManager"));
            row.CanViewCloseout = ParseBool(GetField(fields, headerIndex, "CanViewCloseout"));
            row.CanViewCreateBatch = ParseBool(GetField(fields, headerIndex, "CanViewCreateBatch"));
            row.CanViewDistributionList = ParseBool(GetField(fields, headerIndex, "CanViewDistributionList"));
            row.CanViewGroupManager = ParseBool(GetField(fields, headerIndex, "CanViewGroupManager"));
            row.CanViewHistory = ParseBool(GetField(fields, headerIndex, "CanViewHistory"));
            row.CanViewManageData = ParseBool(GetField(fields, headerIndex, "CanViewManageData"));
            row.CanViewManifestDocuments = ParseBool(GetField(fields, headerIndex, "CanViewManifestDocuments"));
            row.CanViewPickupRequest = ParseBool(GetField(fields, headerIndex, "CanViewPickupRequest"));
            row.CanViewScanAndShip = ParseBool(GetField(fields, headerIndex, "CanViewScanAndShip"));
            row.CanViewShippingAndRating = ParseBool(GetField(fields, headerIndex, "CanViewShippingAndRating"));
            row.CanViewTransmit = ParseBool(GetField(fields, headerIndex, "CanViewTransmit"));

            // Default configuration
            row.ExportFileDelimiter = GetField(fields, headerIndex, "ExportFileDelimiter");
            row.ExportFileQualifier = GetField(fields, headerIndex, "ExportFileQualifier");
            row.ExportFileGroupSeparator = GetField(fields, headerIndex, "ExportFileGroupSeparator");
            row.ExportFileDecimalSeparator = GetField(fields, headerIndex, "ExportFileDecimalSeparator");

            Validate(row);
            rows.Add(row);
        }

        return rows;
    }

    private static void Validate(CsvUserRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Email))
            row.Errors.Add("Email is required.");
        else if (!row.Email.Contains('@') || !row.Email.Contains('.'))
            row.Errors.Add($"'{row.Email}' is not a valid email address.");
    }

    private static bool ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var v = value.Trim();
        return v == "1" ||
               string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
            index.TryAdd(headers[i].Trim(), i);
        return index;
    }

    private static string GetField(string[] fields, Dictionary<string, int> index, string name)
    {
        if (!index.TryGetValue(name, out var i) || i >= fields.Length)
            return string.Empty;
        return fields[i].Trim();
    }

    /// <summary>
    /// Splits a single CSV line respecting double-quoted fields.
    /// </summary>
    public static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        fields.Add(sb.ToString());
        return [.. fields];
    }
}
