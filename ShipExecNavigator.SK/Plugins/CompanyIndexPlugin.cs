using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using ShipExecNavigator.Shared.Models;

namespace ShipExecNavigator.SK.Plugins;

/// <summary>
/// Semantic Kernel plugin that provides entity data from a pre-built company index.
/// This replaces <see cref="EntityXmlPlugin"/> and <see cref="ShipperXmlPlugin"/> so
/// the AI works off a complete, API-sourced index rather than the potentially
/// incomplete serialized XML tree.
/// </summary>
public sealed class CompanyIndexPlugin
{
    private readonly CompanyEntityIndex _index;

    private static readonly Dictionary<string, string> _collectionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Shippers"]                  = "Shipper",
        ["Clients"]                   = "Client",
        ["Profiles"]                  = "Profile",
        ["Sites"]                     = "Site",
        ["Users"]                     = "User",
        ["AdapterRegistrations"]      = "AdapterRegistration",
        ["CarrierRoutes"]             = "CarrierRoute",
        ["ClientBusinessRules"]       = "ClientBusinessRule",
        ["DataConfigurationMappings"] = "DataConfigurationMapping",
        ["DocumentConfigurations"]    = "DocumentConfiguration",
        ["Machines"]                  = "Machine",
        ["PrinterConfigurations"]     = "PrinterConfiguration",
        ["PrinterDefinitions"]        = "PrinterDefinition",
        ["ScaleConfigurations"]       = "ScaleConfiguration",
        ["Schedules"]                 = "Schedule",
        ["ServerBusinessRules"]       = "ServerBusinessRule",
        ["SourceConfigurations"]      = "SourceConfiguration",
    };

    /// <summary>
    /// Categories accessed during the current chat turn's function calls.
    /// Read this after the SK chat completion to determine which tree nodes to expand.
    /// </summary>
    public HashSet<string> AccessedCategories { get; } = new(StringComparer.OrdinalIgnoreCase);

    public CompanyIndexPlugin(CompanyEntityIndex index)
    {
        _index = index;
    }

    // ── Discovery ─────────────────────────────────────────────────────────────

    [KernelFunction("list_entity_types")]
    [Description(
        "Lists all entity categories available in the company configuration with " +
        "entity counts and the id/name of each entity. Use this when you need to " +
        "discover what entity types are present before performing operations on them.")]
    public string ListEntityTypes()
    {
        return $"Company entity manifest:\n{_index.ManifestJson}";
    }

    [KernelFunction("get_category_details")]
    [Description(
        "Returns full details of all entities in a given category as a JSON array " +
        "with all scalar fields. Use this to inspect entity data when you need " +
        "field values beyond id/name (e.g. to answer questions about addresses, " +
        "configuration settings, or cross-references).")]
    public string GetCategoryDetails(
        [Description("Category name (e.g. 'Shippers', 'Profiles', 'Sites', 'Clients')")] string category)
    {
        var json = ResolveCategoryJson(category);
        if (json is null)
            return $"Category '{category}' not found. Call list_entity_types to see available categories.";

        return json;
    }

    // ── Shipper operations ────────────────────────────────────────────────────

    [KernelFunction("find_shippers")]
    [Description(
        "Returns all shipper entries with all fields as a JSON array. " +
        "Use this when the user asks questions about shippers — addresses, names, " +
        "filtering, or any general shipper query.")]
    public string FindShippers(
        [Description("The user's request about shippers")] string userRequest)
    {
        var json = ResolveCategoryJson("Shippers");
        if (json is null)
            return "No shipper data available.";

        return $"Shipper data:\n{json}";
    }

    [KernelFunction("delete_shippers")]
    [Description(
        "Returns all shipper entries with id, symbol, and name. " +
        "Use this when the user asks to DELETE or REMOVE shippers. " +
        "Filter the returned list and respond with action type \"shipper-delete\" " +
        "and payload as a JSON array of matching entries (each with id, symbol, name). " +
        "Do NOT use action type \"javascript\".")]
    public string DeleteShippers(
        [Description("The user's request describing which shippers to delete")] string userRequest)
    {
        var json = ResolveCategoryJson("Shippers");
        if (json is null)
            return "No shipper data available.";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var shippers = doc.RootElement.EnumerateArray()
                .Select(el => new
                {
                    id     = GetField(el, "Id"),
                    symbol = GetField(el, "Symbol"),
                    name   = GetField(el, "Name"),
                })
                .Where(s => !string.IsNullOrEmpty(s.id))
                .ToList();

            var result = JsonSerializer.Serialize(shippers, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {shippers.Count} shipper(s):\n{result}\n\n" +
                   "Filter this list based on the user's condition and respond with " +
                   "action type \"shipper-delete\" and payload as the matching entries " +
                   "(each with id, symbol, name).";
        }
        catch (Exception ex)
        {
            return $"Failed to parse shipper data: {ex.Message}";
        }
    }

    [KernelFunction("edit_shippers")]
    [Description(
        "Returns all shipper entries with all editable fields. " +
        "Use this when the user asks to UPDATE, EDIT, SET, or CHANGE field values on shippers. " +
        "Filter the returned list and respond with action type \"shipper-edit\" " +
        "and payload as a JSON array where each object has id, symbol, name, and edits. " +
        "Do NOT use action type \"javascript\".")]
    public string EditShippers(
        [Description("The user's request describing which shippers to edit and what values to set")] string userRequest)
    {
        var json = ResolveCategoryJson("Shippers");
        if (json is null)
            return "No shipper data available.";

        return $"Shipper data:\n{json}\n\n" +
               "Filter based on the user's condition, determine the field(s) to change, " +
               "and respond with action type \"shipper-edit\" and payload as a JSON array where " +
               "each object has: `id` (string), `symbol` (string), `name` (string), and `edits` " +
               "(an object mapping field names to new values). " +
               "Valid field names: Name, Symbol, Code, Address1, Address2, Address3, City, " +
               "StateProvince, PostalCode, Country, Company, Contact, Phone, Fax, Email, Sms, " +
               "PoBox, Residential.";
    }

    // ── Generic entity operations ─────────────────────────────────────────────

    [KernelFunction("find_entities")]
    [Description(
        "Extracts entities of a given type and returns them as a JSON array with all scalar fields. " +
        "Supported entity types: Profile, Site, User, CarrierRoute, ClientBusinessRule, " +
        "DataConfigurationMapping, DocumentConfiguration, Machine, PrinterConfiguration, " +
        "PrinterDefinition, ScaleConfiguration, Schedule, ServerBusinessRule, SourceConfiguration. " +
        "Use this to inspect entity data before performing delete or edit operations.")]
    public string FindEntities(
        [Description("The singular entity type name (e.g. 'Profile', 'Site', 'Machine')")] string entityType,
        [Description("The user's request describing what to do with the entities")] string userRequest)
    {
        var category = FindCategory(entityType);
        if (category is null)
            return $"Unknown entity type '{entityType}'. Call list_entity_types to see available types.";

        var json = ResolveCategoryJson(category);
        if (json is null)
            return $"No data available for {entityType}.";

        return $"Found {entityType} entities:\n{json}";
    }

    [KernelFunction("delete_entities")]
    [Description(
        "Returns entities of a given type with id and name for deletion. " +
        "Filter the returned list and respond with action type \"entity-delete\" " +
        "and payload as a JSON array where each object has entityType, id, and name. " +
        "Do NOT use action type \"javascript\".")]
    public string DeleteEntities(
        [Description("The singular entity type name (e.g. 'Profile', 'Site', 'Machine')")] string entityType,
        [Description("The user's request describing which entities to delete")] string userRequest)
    {
        var category = FindCategory(entityType);
        if (category is null)
            return $"Unknown entity type '{entityType}'.";

        var json = ResolveCategoryJson(category);
        if (json is null)
            return $"No data available for {entityType}.";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var entities = doc.RootElement.EnumerateArray()
                .Select(el => new
                {
                    id   = GetField(el, "Id"),
                    name = GetField(el, "Name", "UserName", "Symbol", "DisplayName"),
                })
                .Where(e => !string.IsNullOrEmpty(e.id))
                .ToList();

            var result = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {entities.Count} {entityType} entity/entities:\n{result}\n\n" +
                   "Filter this list based on the user's condition and respond with " +
                   $"action type \"entity-delete\" and payload as a JSON array where each object has " +
                   $"`entityType` (set to \"{entityType}\"), `id` (string), and `name` (string).";
        }
        catch (Exception ex)
        {
            return $"Failed to parse entity data: {ex.Message}";
        }
    }

    [KernelFunction("edit_entities")]
    [Description(
        "Returns entities of a given type with all scalar fields for editing. " +
        "Filter the returned list and respond with action type \"entity-edit\" " +
        "and payload as a JSON array where each object has entityType, id, name, and edits. " +
        "Do NOT use action type \"javascript\".")]
    public string EditEntities(
        [Description("The singular entity type name (e.g. 'Profile', 'Site', 'Machine')")] string entityType,
        [Description("The user's request describing which entities to edit and what values to set")] string userRequest)
    {
        var category = FindCategory(entityType);
        if (category is null)
            return $"Unknown entity type '{entityType}'.";

        var json = ResolveCategoryJson(category);
        if (json is null)
            return $"No data available for {entityType}.";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var entities = doc.RootElement.EnumerateArray()
                .Select(el =>
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var prop in el.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                            ? prop.Value.GetString() ?? ""
                            : prop.Value.ToString();
                    }
                    return dict;
                })
                .Where(d => d.Count > 0)
                .ToList();

            var fieldNames = entities.Count > 0
                ? string.Join(", ", entities[0].Keys)
                : "(no entities found)";

            var result = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {entities.Count} {entityType} entity/entities:\n{result}\n\n" +
                   $"Available field names: {fieldNames}\n\n" +
                   "Filter this list based on the user's condition, determine the field(s) to change, " +
                   $"and respond with action type \"entity-edit\" and payload as a JSON array where " +
                   $"each object has: `entityType` (set to \"{entityType}\"), `id` (string), `name` (string), " +
                   "and `edits` (an object mapping field names to new values).";
        }
        catch (Exception ex)
        {
            return $"Failed to parse entity data: {ex.Message}";
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private string? ResolveCategoryJson(string category)
    {
        if (_index.CategoryDetailsJson.TryGetValue(category, out var json))
        {
            AccessedCategories.Add(category);
            return json;
        }

        var key = _index.CategoryDetailsJson.Keys
            .FirstOrDefault(k => k.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (key is not null)
        {
            AccessedCategories.Add(key);
            return _index.CategoryDetailsJson[key];
        }
        return null;
    }

    private string? FindCategory(string entityType)
    {
        foreach (var (plural, singular) in _collectionMap)
        {
            if (singular.Equals(entityType, StringComparison.OrdinalIgnoreCase))
            {
                AccessedCategories.Add(plural);
                return plural;
            }
        }
        if (_collectionMap.ContainsKey(entityType))
        {
            AccessedCategories.Add(entityType);
            return entityType;
        }
        return null;
    }

    private static string GetField(JsonElement el, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            if (el.TryGetProperty(name, out var prop))
            {
                var value = prop.ValueKind == JsonValueKind.String
                    ? prop.GetString()
                    : prop.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }
        return "";
    }
}
