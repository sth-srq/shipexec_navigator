namespace ShipExecAgent.Shared.Models;

public class CsvUserRow
{
    public int RowNumber { get; set; }

    // Identity / account
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IdLevel { get; set; } = string.Empty;
    public string ProfileName { get; set; } = string.Empty;

    // Campus / site
    public string Campus { get; set; } = string.Empty;
    public string CampusDisplayName { get; set; } = string.Empty;

    // Name / address
    public string Company { get; set; } = string.Empty;
    public string FName { get; set; } = string.Empty;
    public string LName { get; set; } = string.Empty;
    public string Addr1 { get; set; } = string.Empty;
    public string Addr2 { get; set; } = string.Empty;
    public string Addr3 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    // Custom attributes
    public string Custom1 { get; set; } = string.Empty;
    public string Custom2 { get; set; } = string.Empty;
    public string Custom3 { get; set; } = string.Empty;
    public string Custom4 { get; set; } = string.Empty;
    public string Custom5 { get; set; } = string.Empty;
    public string Custom6 { get; set; } = string.Empty;
    public string Custom7 { get; set; } = string.Empty;
    public string Custom8 { get; set; } = string.Empty;
    public string Custom9 { get; set; } = string.Empty;
    public string Custom10 { get; set; } = string.Empty;

    // Permissions (CanView* flags)
    public bool CanViewAddressBook { get; set; }
    public bool CanViewBatchManager { get; set; }
    public bool CanViewCloseout { get; set; }
    public bool CanViewCreateBatch { get; set; }
    public bool CanViewDistributionList { get; set; }
    public bool CanViewGroupManager { get; set; }
    public bool CanViewHistory { get; set; }
    public bool CanViewManageData { get; set; }
    public bool CanViewManifestDocuments { get; set; }
    public bool CanViewPickupRequest { get; set; }
    public bool CanViewScanAndShip { get; set; }
    public bool CanViewShippingAndRating { get; set; }
    public bool CanViewTransmit { get; set; }

    // Default configuration (enum names)
    public string ExportFileDelimiter { get; set; } = string.Empty;
    public string ExportFileQualifier { get; set; } = string.Empty;
    public string ExportFileGroupSeparator { get; set; } = string.Empty;
    public string ExportFileDecimalSeparator { get; set; } = string.Empty;

    // Validation
    public List<string> Errors { get; set; } = [];
    public bool IsValid => Errors.Count == 0;
}
