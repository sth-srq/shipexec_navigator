using System.Text.Json;

namespace ShipExecAgent.Helpers;

public record FieldDiffRow(string Name, string? OrigVal, string? NewVal, bool IsChanged);

public static class FieldDiffHelper
{
    /// <summary>
    /// Builds a field-level diff from two JSON objects.
    /// When <paramref name="includeUnchanged"/> is true every field is returned;
    /// changed fields have <see cref="FieldDiffRow.IsChanged"/> = true.
    /// </summary>
    public static List<FieldDiffRow> BuildFieldDiff(
        string changeType,
        string origXml,
        string newXml,
        bool includeUnchanged = false)
    {
        var rows = new List<FieldDiffRow>();
        try
        {
            Dictionary<string, JsonElement>? orig    = null;
            Dictionary<string, JsonElement>? newDict = null;

            if (!string.IsNullOrEmpty(origXml))
                orig    = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(origXml);
            if (!string.IsNullOrEmpty(newXml))
                newDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(newXml);

            if (orig is null && newDict is null) return rows;

            // Key order: new-object keys first, then any keys only in orig
            var keys = newDict is not null ? new List<string>(newDict.Keys) : new List<string>();
            keys.AddRange(
                (orig?.Keys ?? Enumerable.Empty<string>())
                    .Where(k => !keys.Contains(k, StringComparer.OrdinalIgnoreCase)));

            bool isUpdate = changeType.Equals("update",   StringComparison.OrdinalIgnoreCase) ||
                            changeType.Equals("modified", StringComparison.OrdinalIgnoreCase);

            foreach (var key in keys)
            {
                JsonElement oe = default, ne = default;
                var hasOrig = orig    is not null && orig.TryGetValue(key,    out oe);
                var hasNew  = newDict is not null && newDict.TryGetValue(key, out ne);

                // Skip nested objects and arrays — only scalar fields produce meaningful diff rows
                if ((hasOrig && (oe.ValueKind == JsonValueKind.Object || oe.ValueKind == JsonValueKind.Array)) ||
                    (hasNew  && (ne.ValueKind == JsonValueKind.Object || ne.ValueKind == JsonValueKind.Array)))
                    continue;

                var origVal = hasOrig ? GetJsonString(oe) : null;
                var newVal  = hasNew  ? GetJsonString(ne) : null;

                if (string.IsNullOrEmpty(origVal) && string.IsNullOrEmpty(newVal)) continue;

                // Normalise for comparison: trim surrounding whitespace
                var trimmedOrig = (origVal ?? string.Empty).Trim();
                var trimmedNew  = (newVal  ?? string.Empty).Trim();

                // If both sides are non-empty and look identical, it is never a change —
                // this guards against invisible differences (whitespace, encoding) AND
                // against an unexpected changeType that leaves isUpdate = false.
                bool valuesMatch = !string.IsNullOrEmpty(origVal) &&
                                   !string.IsNullOrEmpty(newVal)  &&
                                   string.Equals(trimmedOrig, trimmedNew, StringComparison.Ordinal);

                bool isChanged = !valuesMatch &&
                                 (!isUpdate || !string.Equals(trimmedOrig, trimmedNew, StringComparison.Ordinal));

                if (!includeUnchanged && !isChanged) continue;

                rows.Add(new FieldDiffRow(key, origVal, newVal, isChanged));
            }
        }
        catch { /* return whatever was built so far */ }
        return rows;
    }

    public static List<FieldDiffRow> GetChangedFields(string origXml, string newXml) =>
        BuildFieldDiff("update", origXml, newXml);

    public static string GetJsonString(JsonElement elem) => elem.ValueKind switch
    {
        JsonValueKind.String => elem.GetString() ?? string.Empty,
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        JsonValueKind.Null   => string.Empty,
        JsonValueKind.Number => elem.ToString(),
        _                    => elem.GetRawText()
    };
}
