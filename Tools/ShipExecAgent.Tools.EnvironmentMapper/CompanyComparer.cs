using System.IO;
using System.Xml.Linq;

namespace ShipExecAgent.Tools.EnvironmentMapper;

/// <summary>
/// Compares two PSI.Sox Company XML exports from different environments and
/// produces an ID mapping based on matching entities by their non-ID identity
/// fields (Name, Symbol, Alias, Guid, etc.).
///
/// A two-pass approach resolves foreign-key-heavy entities (e.g. PickupType)
/// that share the same name/symbol but differ only by carrier or adapter IDs.
/// </summary>
public class CompanyComparer
{
    private static readonly string[] IdentityFieldNames =
        ["Name", "Symbol", "Alias", "Guid", "Key"];

    private static readonly HashSet<string> SkipForeignKeys =
        new(StringComparer.OrdinalIgnoreCase) { "CompanyId" };

    private readonly Action<string> _log;

    // Accumulated Id mapping: (File1 Id) → (File2 Id).
    // Built during pass 1 so that pass 2 can resolve foreign keys.
    private readonly Dictionary<string, string> _idMap = new(StringComparer.OrdinalIgnoreCase);

    // Deferred matches: entities that could not be uniquely matched in pass 1.
    private readonly List<DeferredMatch> _deferred = [];

    public CompanyComparer(Action<string> log) => _log = log;

    public List<IdMapping> Compare(string file1Path, string file2Path)
    {
        _log($"Source : {Path.GetFileName(file1Path)}");
        _log($"Target : {Path.GetFileName(file2Path)}");
        _log("");

        var doc1 = XDocument.Load(file1Path);
        var doc2 = XDocument.Load(file2Path);

        var results = new List<IdMapping>();

        // Pass 1 — match by identity fields only (Name, Symbol, etc.)
        CompareMatchedElements(doc1.Root!, doc2.Root!, "Company", results);

        // Pass 2 — re-attempt deferred entities using FK resolution
        if (_deferred.Count > 0)
        {
            int resolved = 0;
            foreach (var d in _deferred)
                resolved += RetryWithResolvedForeignKeys(d, results);

            if (resolved > 0)
                _log($"  ℹ Pass 2 resolved {resolved} additional mapping(s) via foreign-key lookup.");
        }

        return results;
    }

    // ------------------------------------------------------------------
    //  Core recursive comparison of two matched elements
    // ------------------------------------------------------------------
    private void CompareMatchedElements(
        XElement e1, XElement e2, string path, List<IdMapping> results)
    {
        var display = GetDisplayName(e1);

        // 1. Map the primary Id
        var id1 = TextChild(e1, "Id");
        var id2 = TextChild(e2, "Id");
        if (id1 != null && id2 != null)
        {
            results.Add(new IdMapping(path, display, "Id", id1, id2));
            _idMap[id1] = id2;
        }

        // 2. Map foreign-key fields (*Id) that differ
        foreach (var child1 in e1.Elements())
        {
            var name = child1.Name.LocalName;
            if (!name.EndsWith("Id", StringComparison.Ordinal)) continue;
            if (name == "Id" || SkipForeignKeys.Contains(name)) continue;
            if (child1.HasElements) continue;

            var v1 = child1.Value.Trim();
            var child2 = e2.Element(name);
            if (child2 == null || child2.HasElements) continue;
            var v2 = child2.Value.Trim();

            if (v1.Length > 0 && v2.Length > 0 && v1 != v2)
            {
                results.Add(new IdMapping(path, display, name, v1, v2));
                _idMap.TryAdd(v1, v2);
            }
        }

        // 3. Recurse into child wrappers
        foreach (var wrapper1 in e1.Elements().Where(c => c.HasElements))
        {
            var wrapperName = wrapper1.Name.LocalName;
            if (wrapperName.EndsWith("Id", StringComparison.Ordinal)) continue;

            var wrapper2 = e2.Element(wrapperName);
            if (wrapper2 == null || !wrapper2.HasElements) continue;

            // 3a. Collection of entities (children that have their own <Id>)
            var groups1 = wrapper1.Elements()
                .Where(e => e.Element("Id") != null)
                .GroupBy(e => e.Name.LocalName)
                .ToList();

            if (groups1.Count > 0)
            {
                foreach (var grp in groups1)
                {
                    var list1 = grp.ToList();
                    var list2 = wrapper2.Elements(grp.Key)
                        .Where(e => e.Element("Id") != null)
                        .ToList();

                    var childPath = $"{path} > {wrapperName} > {grp.Key}";
                    MatchEntities(list1, list2, childPath, results);
                }
            }
            // 3b. Single entity child (e.g., ProfileSettings inside Profile)
            else if (wrapper1.Element("Id") != null && wrapper2.Element("Id") != null)
            {
                var childPath = $"{path} > {wrapperName}";
                CompareMatchedElements(wrapper1, wrapper2, childPath, results);
            }
        }
    }

    // ------------------------------------------------------------------
    //  Match two lists of same-typed entities and recurse into each pair
    // ------------------------------------------------------------------
    private void MatchEntities(
        List<XElement> list1, List<XElement> list2, string path, List<IdMapping> results)
    {
        var used = new HashSet<int>();
        var unmatched1 = new List<XElement>();

        foreach (var item1 in list1)
        {
            var sig1 = BuildSignature(item1);
            int bestIdx = -1;
            int bestScore = 0;

            for (int i = 0; i < list2.Count; i++)
            {
                if (used.Contains(i)) continue;
                int score = ScoreMatch(sig1, BuildSignature(list2[i]));
                if (score > bestScore) { bestScore = score; bestIdx = i; }
            }

            if (bestIdx >= 0 && bestScore > 0)
            {
                used.Add(bestIdx);
                CompareMatchedElements(item1, list2[bestIdx], path, results);
            }
            else
            {
                unmatched1.Add(item1);
            }
        }

        var unmatched2 = new List<XElement>();
        for (int i = 0; i < list2.Count; i++)
            if (!used.Contains(i))
                unmatched2.Add(list2[i]);

        // Defer unmatched items for pass 2 (FK-resolved matching)
        if (unmatched1.Count > 0 && unmatched2.Count > 0)
        {
            _deferred.Add(new DeferredMatch(path, unmatched1, unmatched2));
        }
        else
        {
            foreach (var item in unmatched1)
                _log($"  ⚠ Unmatched in source: {path} \"{GetDisplayName(item)}\" (Id={TextChild(item, "Id")})");
            foreach (var item in unmatched2)
                _log($"  ⚠ Unmatched in target: {path} \"{GetDisplayName(item)}\" (Id={TextChild(item, "Id")})");
        }
    }

    // ------------------------------------------------------------------
    //  Pass 2: FK-resolved matching for deferred entities
    // ------------------------------------------------------------------
    private int RetryWithResolvedForeignKeys(DeferredMatch d, List<IdMapping> results)
    {
        var used = new HashSet<int>();
        int resolved = 0;

        foreach (var item1 in d.Unmatched1)
        {
            var sig1 = BuildResolvedSignature(item1);
            int bestIdx = -1;
            int bestScore = 0;

            for (int i = 0; i < d.Unmatched2.Count; i++)
            {
                if (used.Contains(i)) continue;
                int score = ScoreMatch(sig1, BuildResolvedSignature(d.Unmatched2[i]));
                if (score > bestScore) { bestScore = score; bestIdx = i; }
            }

            if (bestIdx >= 0 && bestScore > 0)
            {
                used.Add(bestIdx);
                CompareMatchedElements(item1, d.Unmatched2[bestIdx], d.Path, results);
                resolved++;
            }
            else
            {
                _log($"  ⚠ Unmatched in source: {d.Path} \"{GetDisplayName(item1)}\" (Id={TextChild(item1, "Id")})");
            }
        }

        for (int i = 0; i < d.Unmatched2.Count; i++)
            if (!used.Contains(i))
                _log($"  ⚠ Unmatched in target: {d.Path} \"{GetDisplayName(d.Unmatched2[i])}\" (Id={TextChild(d.Unmatched2[i], "Id")})");

        return resolved;
    }

    /// <summary>
    /// Like <see cref="BuildSignature"/> but also includes foreign-key fields
    /// after resolving the source-environment value through the already-built
    /// <see cref="_idMap"/>. This lets entities that differ only by FK values
    /// (e.g. PickupType.CarrierId) compare as equal when the referenced entity
    /// was already mapped in pass 1.
    /// </summary>
    private Dictionary<string, string> BuildResolvedSignature(XElement element)
    {
        var sig = BuildSignature(element);

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;
            if (name == "Id" || !name.EndsWith("Id", StringComparison.Ordinal)) continue;
            if (SkipForeignKeys.Contains(name)) continue;
            if (child.HasElements) continue;

            var rawValue = child.Value.Trim();
            if (rawValue.Length == 0) continue;

            // Resolve to the target-environment equivalent
            var resolvedValue = _idMap.TryGetValue(rawValue, out var mapped) ? mapped : rawValue;
            sig[$"_FK_{name}"] = resolvedValue;
        }

        return sig;
    }

    // ------------------------------------------------------------------
    //  Signature: a dictionary of non-ID field values used for matching
    // ------------------------------------------------------------------
    private static Dictionary<string, string> BuildSignature(XElement element)
    {
        var sig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var child in element.Elements())
        {
            var name = child.Name.LocalName;

            // Skip primary key and foreign key fields
            if (name == "Id" || name.EndsWith("Id", StringComparison.Ordinal))
                continue;

            if (!child.HasElements)
            {
                // Simple text value
                var value = child.Value.Trim();
                if (value.Length > 0 && value.Length < 200)
                    sig[name] = value;
            }
            else
            {
                // Complex child — extract identity fields one level down
                foreach (var fieldName in IdentityFieldNames)
                {
                    var gc = child.Element(fieldName);
                    if (gc != null && !gc.HasElements)
                    {
                        var val = gc.Value.Trim();
                        if (val.Length > 0)
                            sig[$"{name}.{fieldName}"] = val;
                    }
                }
            }
        }

        return sig;
    }

    private static int ScoreMatch(
        Dictionary<string, string> sig1, Dictionary<string, string> sig2)
    {
        int score = 0;
        foreach (var kvp in sig1)
        {
            if (sig2.TryGetValue(kvp.Key, out var v) &&
                string.Equals(kvp.Value, v, StringComparison.OrdinalIgnoreCase))
            {
                var field = kvp.Key.Contains('.')
                    ? kvp.Key[(kvp.Key.LastIndexOf('.') + 1)..]
                    : kvp.Key;
                score += Array.Exists(IdentityFieldNames,
                    f => f.Equals(field, StringComparison.OrdinalIgnoreCase)) ? 10 : 1;
            }
        }
        return score;
    }

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------
    private static string GetDisplayName(XElement e)
    {
        // 1. Direct identity field (Name, Symbol, Alias, etc.)
        foreach (var f in IdentityFieldNames)
        {
            var v = TextChild(e, f);
            if (v != null) return v;
        }

        // 2. Look one level deeper — find identity fields inside complex children
        //    (e.g., AdapterRegistration → AdapterDefinition.Name)
        foreach (var child in e.Elements().Where(c => c.HasElements))
        {
            foreach (var f in IdentityFieldNames)
            {
                var v = TextChild(child, f);
                if (v != null) return v;
            }
        }

        return TextChild(e, "Id") ?? e.Name.LocalName;
    }

    private static string? TextChild(XElement e, string name)
    {
        var c = e.Element(name);
        if (c == null || c.HasElements) return null;
        var v = c.Value.Trim();
        return v.Length == 0 ? null : v;
    }

    private record DeferredMatch(string Path, List<XElement> Unmatched1, List<XElement> Unmatched2);
}
