using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace ShipExecNavigator.SK.Plugins;

/// <summary>
/// Semantic Kernel plugin that surfaces user data so the AI can produce
/// a structured <c>user-find</c> response for the Navigator Users tab.
/// </summary>
public sealed class UserXmlPlugin
{
    private readonly string _usersJson;
    private readonly string _userMetaJson;

    public UserXmlPlugin(string usersJson, string userMetaJson = "{}")
    {
        _usersJson    = usersJson;
        _userMetaJson = userMetaJson;
    }

    [KernelFunction("find_users")]
    [Description(
        "Returns the list of users from the loaded ShipExec company as a JSON array with " +
        "id, username, email, profileId, and profileName. Use this when the user asks to FIND, " +
        "SEARCH, or FILTER users, or when asking about users per profile, user counts by profile, " +
        "or any profile-user relationship. " +
        "You MUST then filter the returned list to find matching users and respond with a " +
        "```user-find code block containing a JSON array of the matching entries.")]
    public string FindUsers(
        [Description("The user's request describing which users to find")] string userRequest)
    {
        try
        {
            using var doc = JsonDocument.Parse(_usersJson);
            var users = doc.RootElement.EnumerateArray()
                .Select(el => new
                {
                    id          = el.TryGetProperty("Id",          out var idProp)  ? idProp.GetString()  ?? "" : "",
                    username    = el.TryGetProperty("UserName",    out var unProp)  ? unProp.GetString()  ?? "" : "",
                    email       = el.TryGetProperty("Email",       out var emProp)  ? emProp.GetString()  ?? "" : "",
                    profileId   = el.TryGetProperty("ProfileId",   out var piProp)  ? piProp.ToString()       : "",
                    profileName = el.TryGetProperty("ProfileName", out var pnProp)  ? pnProp.GetString() ?? "" : "",
                })
                .Where(u => !string.IsNullOrEmpty(u.id))
                .ToList();

            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {users.Count} user(s):\n{json}\n\n" +
                   "Filter this list based on the user's condition and respond with a " +
                   "```user-find code block containing ONLY the JSON array of matching entries " +
                   "(each object must have `id`, `username`, and `email` string fields, " +
                   "exactly as returned by this function). " +
                   "You can group or count users by profileId/profileName to answer profile-related questions. " +
                   "Do NOT include any JavaScript — the Navigator will highlight those users directly.";
        }
        catch (Exception ex)
        {
            return $"Failed to parse users: {ex.Message}";
        }
    }

    [KernelFunction("edit_users")]
    [Description(
        "Returns the list of users AND the available permissions/roles for the company. " +
        "Use this when the user asks to UPDATE, EDIT, SET, or CHANGE ANY field on users " +
        "(address fields, configuration settings, permissions, roles, etc.). " +
        "You MUST then filter the returned user list to find matching users and respond with a " +
        "```user-edit code block containing a JSON array of the matching entries with their edits.")]
    public string EditUsers(
        [Description("The user's request describing which users to edit and what to change")] string userRequest)
    {
        try
        {
            using var doc = JsonDocument.Parse(_usersJson);
            var users = doc.RootElement.EnumerateArray()
                .Select(el => new
                {
                    id       = el.TryGetProperty("Id",       out var idProp) ? idProp.GetString()       ?? "" : "",
                    username = el.TryGetProperty("UserName", out var unProp) ? unProp.GetString()       ?? "" : "",
                    email    = el.TryGetProperty("Email",    out var emProp) ? emProp.GetString()       ?? "" : "",
                })
                .Where(u => !string.IsNullOrEmpty(u.id))
                .ToList();

            var usersJson = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });

            return $"Found {users.Count} user(s):\n{usersJson}\n\n" +
                   $"Company permissions and roles available:\n{_userMetaJson}\n\n" +
                   "Filter the user list based on the user's condition and respond with a " +
                   "```user-edit code block containing ONLY the JSON array of matching entries. " +
                   "Each object must have `id` (string), `username` (string), `email` (string), and " +
                   "`edits` (an object). Supported edit fields:\n" +
                   "Top-level: Email, UserName, PhoneNumber, PasswordExpired (bool string), LockoutEnabled (bool string), EmailConfirmed (bool string), PhoneNumberConfirmed (bool string)\n" +
                   "Address: Address.Company, Address.Contact, Address.Address1, Address.Address2, Address.Address3, Address.City, Address.StateProvince, Address.PostalCode, Address.Country, Address.Phone, Address.Fax, Address.Email, Address.Sms, Address.Account, Address.TaxId, Address.Code, Address.Group, Address.PoBox (bool string), Address.Residential (bool string)\n" +
                   "Config: Config.ExportFileDelimiter (Comma/Semicolon/Tab), Config.ExportFileQualifier (None/DoubleQuotes/SingleQuote), Config.ExportFileGroupSeparator (Comma/Period), Config.ExportFileDecimalSeparator (Comma/Period)\n" +
                   "Permissions: Permissions.Add (exact permission name from availablePermissions), Permissions.Remove (exact permission name)\n" +
                   "Roles: Roles.Add (exact role name from availableRoles), Roles.Remove (exact role name)\n" +
                   "Do NOT include any JavaScript — the Navigator will apply the edits directly.";
        }
        catch (Exception ex)
        {
            return $"Failed to parse users: {ex.Message}";
        }
    }

    [KernelFunction("delete_users")]
    [Description(
        "Returns the list of users from the loaded ShipExec company as a JSON array with " +
        "id, username, and email. Use this when the user asks to DELETE or REMOVE users. " +
        "You MUST then filter the returned list to find matching users and respond with a " +
        "```user-delete code block containing a JSON array of the matching entries.")]
    public string DeleteUsers(
        [Description("The user's request describing which users to delete")] string userRequest)
    {
        try
        {
            using var doc = JsonDocument.Parse(_usersJson);
            var users = doc.RootElement.EnumerateArray()
                .Select(el => new
                {
                    id       = el.TryGetProperty("Id",       out var idProp) ? idProp.GetString()       ?? "" : "",
                    username = el.TryGetProperty("UserName", out var unProp) ? unProp.GetString()       ?? "" : "",
                    email    = el.TryGetProperty("Email",    out var emProp) ? emProp.GetString()       ?? "" : "",
                })
                .Where(u => !string.IsNullOrEmpty(u.id))
                .ToList();

            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {users.Count} user(s):\n{json}\n\n" +
                   "Filter this list based on the user's condition and respond with a " +
                   "```user-delete code block containing ONLY the JSON array of matching entries " +
                   "(each object must have `id`, `username`, and `email` string fields, " +
                   "exactly as returned by this function). " +
                   "Do NOT include any JavaScript — the Navigator will delete those users directly.";
        }
        catch (Exception ex)
        {
            return $"Failed to parse users: {ex.Message}";
        }
    }
}
