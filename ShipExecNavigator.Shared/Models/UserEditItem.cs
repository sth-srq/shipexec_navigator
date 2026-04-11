namespace ShipExecNavigator.Shared.Models;

/// <summary>
/// Represents a user field-edit identified by the AI chat.
/// Each item targets one user and carries a dictionary of field → value changes.
/// </summary>
public class UserEditItem
{
    public string Id       { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;

    /// <summary>
    /// Field-name → new-value pairs to apply (case-insensitive keys).
    ///
    /// Top-level:   Email, UserName, PhoneNumber, PasswordExpired (bool), LockoutEnabled (bool),
    ///              EmailConfirmed (bool), PhoneNumberConfirmed (bool)
    ///
    /// Address:     Address.Company, Address.Contact, Address.Address1–3, Address.City,
    ///              Address.StateProvince, Address.PostalCode, Address.Country, Address.Phone,
    ///              Address.Fax, Address.Email, Address.Sms, Address.Account, Address.TaxId,
    ///              Address.Code, Address.Group, Address.PoBox (bool), Address.Residential (bool)
    ///
    /// Config:      Config.ExportFileDelimiter (Comma/Semicolon/Tab)
    ///              Config.ExportFileQualifier (None/DoubleQuotes/SingleQuote)
    ///              Config.ExportFileGroupSeparator (Comma/Period)
    ///              Config.ExportFileDecimalSeparator (Comma/Period)
    ///
    /// Permissions: Permissions.Add (permission name), Permissions.Remove (permission name)
    /// Roles:       Roles.Add (role name), Roles.Remove (role name)
    ///
    /// Unqualified address shorthands (backwards-compat): Company, Contact, Address1–3,
    ///              City, StateProvince, PostalCode, Country, Phone, Fax
    /// </summary>
    public Dictionary<string, string> Edits { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
