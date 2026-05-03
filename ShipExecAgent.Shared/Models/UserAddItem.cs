namespace ShipExecAgent.Shared.Models;

/// <summary>
/// Represents a new user to be created, as identified by the AI chat.
/// Field values are optional; the CreateUserDialog will display them
/// pre-populated and let the user supply the password and confirm before committing.
/// </summary>
public class UserAddItem
{
    /// <summary>Login e-mail / username (used as both UserName and Email in ShipExec).</summary>
    public string Email { get; set; } = string.Empty;

    // ── Address book fields ──────────────────────────────────────────────────
    public string Company { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string Address2 { get; set; } = string.Empty;
    public string Address3 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
}
