namespace ShipExecNavigator;

/// <summary>
/// Development-only convenience flags. Flip <see cref="AutoConnect"/> to <c>true</c>
/// to skip the manual connect flow during local ShipExecNavigator.
/// ⚠ Never commit with AutoConnect = true.
/// </summary>
internal static class DebugConfig
{
    /// <summary>
    /// When true the app opens the Connect dialog automatically on launch and
    /// auto-selects the <see cref="AutoSelectCompany"/> once the company list loads.
    /// </summary>
    public const bool AutoConnect = false;

    /// <summary>Admin API base URL used during auto-connect.</summary>
    internal const string AdminUrl =
        "https://BLACKBOX23/ShipExecManagementStudioApi/api/AdministrationService/";

    /// <summary>JWT JSON for auto-connect login. Do not commit.</summary>
    internal const string JwtToken = "";

    /// <summary>Company name (or display name) to auto-select from the list.</summary>
    internal const string AutoSelectCompany = "WebTest";
}
