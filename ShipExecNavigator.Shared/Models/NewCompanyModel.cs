namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Holds all data collected during the New Company wizard flow.
/// </summary>
public class NewCompanyModel
{
    // ── Step 1: General ──────────────────────────────────────────────────────
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;

    // ── Step 2: Shippers (optional) ──────────────────────────────────────────
    public string? ShipperCsvContent { get; set; }
    public string? ShipperFileName { get; set; }

    // ── Step 3: Users (optional) ─────────────────────────────────────────────
    public string? UserCsvContent { get; set; }
    public string? UserFileName { get; set; }

    // ── Step 4: Blueprint (optional) ─────────────────────────────────────────
    public byte[]? BlueprintFileContent { get; set; }
    public string? BlueprintFileName { get; set; }

    // ── Step 6: Credentials (required for import when not connected) ─────────
    public string? AdminUrl { get; set; }
    public string? JwtJson { get; set; }
}

/// <summary>
/// Result returned after attempting to create a company via the API.
/// </summary>
public class CreateCompanyResult
{
    public bool Success { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? ErrorMessage { get; set; }
}
