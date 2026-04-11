using System.ComponentModel;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.SemanticKernel;

namespace ShipExecNavigator.SK.Plugins;

/// <summary>
/// Semantic Kernel plugin that analyses the loaded ShipExec XML and surfaces
/// any entity type so the AI can produce structured delete/edit responses for
/// entities beyond Shippers, Clients, and AdapterRegistrations (e.g. Profiles,
/// Sites, CarrierRoutes, Machines, Schedules, etc.).
/// </summary>
public sealed class EntityXmlPlugin
{
    private readonly string _xmlContent;

    /// <summary>
    /// Maps plural collection name → singular child element name.
    /// </summary>
    private static readonly Dictionary<string, string> _collectionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Profiles"]                  = "Profile",
        ["Sites"]                     = "Site",
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

    public EntityXmlPlugin(string xmlContent)
    {
        _xmlContent = xmlContent;
    }

    [KernelFunction("list_entity_types")]
    [Description(
        "Lists all entity collection types available in the loaded XML configuration " +
        "and how many items each contains. Use this when you need to discover what entity " +
        "types are present before performing operations on them.")]
    public string ListEntityTypes()
    {
        try
        {
            var doc = XDocument.Parse(_xmlContent);
            var results = new List<object>();

            foreach (var (plural, singular) in _collectionMap)
            {
                var count = doc.Descendants(singular).Count();
                if (count > 0)
                    results.Add(new { collectionName = plural, entityType = singular, count });
            }

            var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
            return $"Available entity types in the XML:\n{json}";
        }
        catch (Exception ex)
        {
            return $"Failed to parse XML: {ex.Message}";
        }
    }

    [KernelFunction("find_entities")]
    [Description(
        "Extracts entities of a given type from the loaded ShipExec XML configuration and returns them " +
        "as a JSON array. Each entry includes `id`, `name`, and all scalar child element values. " +
        "Supported entity types: Profile, Site, CarrierRoute, DataConfigurationMapping, " +
        "DocumentConfiguration, Machine, PrinterConfiguration, PrinterDefinition, " +
        "ScaleConfiguration, Schedule, SourceConfiguration. " +
        "Use this to inspect entity data before performing delete or edit operations.")]
    public string FindEntities(
        [Description("The singular entity type name (e.g. 'Profile', 'Site', 'Machine')")] string entityType,
        [Description("The user's request describing what to do with the entities")] string userRequest)
    {
        try
        {
            var doc = XDocument.Parse(_xmlContent);
            var entities = doc.Descendants(entityType)
                .Select(el =>
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var child in el.Elements())
                    {
                        if (!child.HasElements)
                            dict[child.Name.LocalName] = child.Value;
                    }
                    return dict;
                })
                .Where(d => d.Count > 0)
                .ToList();

            var json = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {entities.Count} {entityType} entity/entities in the XML:\n{json}";
        }
        catch (Exception ex)
        {
            return $"Failed to parse XML: {ex.Message}";
        }
    }

    [KernelFunction("delete_entities")]
    [Description(
        "Extracts entities of a given type from the loaded ShipExec XML configuration and returns them " +
        "as a JSON array with id and name. Use this when the user asks to DELETE or REMOVE entities " +
        "of types: Profile, Site, CarrierRoute, DataConfigurationMapping, DocumentConfiguration, " +
        "Machine, PrinterConfiguration, PrinterDefinition, ScaleConfiguration, Schedule, " +
        "SourceConfiguration. You MUST then filter the returned list to find matching entities " +
        "and respond with action type \"entity-delete\" and payload as a JSON array where each " +
        "object has `entityType`, `id`, and `name` string fields.")]
    public string DeleteEntities(
        [Description("The singular entity type name (e.g. 'Profile', 'Site', 'Machine')")] string entityType,
        [Description("The user's request describing which entities to delete")] string userRequest)
    {
        try
        {
            var doc = XDocument.Parse(_xmlContent);
            var entities = doc.Descendants(entityType)
                .Select(el => new
                {
                    id   = (string?)el.Element("Id")   ?? "",
                    name = (string?)el.Element("Name") ?? (string?)el.Element("Symbol") ?? (string?)el.Element("DisplayName") ?? "",
                })
                .Where(e => !string.IsNullOrEmpty(e.id))
                .ToList();

            var json = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {entities.Count} {entityType} entity/entities in the XML:\n{json}\n\n" +
                   "Filter this list based on the user's condition and respond with " +
                   $"action type \"entity-delete\" and payload as a JSON array where each object has " +
                   $"`entityType` (set to \"{entityType}\"), `id` (string), and `name` (string).";
        }
        catch (Exception ex)
        {
            return $"Failed to parse XML: {ex.Message}";
        }
    }

    [KernelFunction("edit_entities")]
    [Description(
        "Extracts entities of a given type from the loaded ShipExec XML configuration and returns them " +
        "as a JSON array with all scalar fields. Use this when the user asks to UPDATE, EDIT, SET, or " +
        "CHANGE field values on entities of types: Profile, Site, CarrierRoute, " +
        "DataConfigurationMapping, DocumentConfiguration, Machine, PrinterConfiguration, " +
        "PrinterDefinition, ScaleConfiguration, Schedule, SourceConfiguration. " +
        "You MUST then filter the returned list, determine which fields to change, and respond with " +
        "action type \"entity-edit\" and payload as a JSON array of edit objects.")]
    public string EditEntities(
        [Description("The singular entity type name (e.g. 'Profile', 'Site', 'Machine')")] string entityType,
        [Description("The user's request describing which entities to edit and what values to set")] string userRequest)
    {
        try
        {
            var doc = XDocument.Parse(_xmlContent);
            var entities = doc.Descendants(entityType)
                .Select(el =>
                {
                    var dict = new Dictionary<string, string>();
                    foreach (var child in el.Elements())
                    {
                        if (!child.HasElements)
                            dict[child.Name.LocalName] = child.Value;
                    }
                    return dict;
                })
                .Where(d => d.Count > 0)
                .ToList();

            // Collect available field names from the first entity
            var fieldNames = entities.Count > 0
                ? string.Join(", ", entities[0].Keys)
                : "(no entities found)";

            var json = JsonSerializer.Serialize(entities, new JsonSerializerOptions { WriteIndented = true });
            return $"Found {entities.Count} {entityType} entity/entities in the XML:\n{json}\n\n" +
                   $"Available field names: {fieldNames}\n\n" +
                   "Filter this list based on the user's condition, determine the field(s) to change, " +
                   $"and respond with action type \"entity-edit\" and payload as a JSON array where " +
                   $"each object has: `entityType` (set to \"{entityType}\"), `id` (string), `name` (string), " +
                   "and `edits` (an object mapping field names to new values).";
        }
        catch (Exception ex)
        {
            return $"Failed to parse XML: {ex.Message}";
        }
    }
}
