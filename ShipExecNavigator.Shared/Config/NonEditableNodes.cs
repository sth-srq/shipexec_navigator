namespace ShipExecNavigator.Shared.Config;

/// <summary>
/// Defines node paths where inline editing of values and attributes is disabled.
/// The restriction cascades automatically to all descendants.
/// Context-menu Add / Remove operations are NOT affected — only inline editing.
///
/// Paths are dot-separated node names and support two formats:
///
///   Full path   — "Company.Profiles.Profile.Shipper"
///                 Only matches nodes at exactly that position from the root.
///
///   Suffix path — "Profile.Shipper"
///                 Matches any node whose path ENDS WITH those segments,
///                 regardless of how many ancestor levels precede them.
///                 This is the recommended format because it works even when
///                 you are unsure of the exact root structure.
///
/// To discover the exact full path of any node, inspect the element in your
/// browser DevTools — every node div carries a data-nodepath attribute with
/// the full computed path (e.g. data-nodepath="Company.Profiles.Profile.Shippers.Shipper").
///
/// To add more non-editable paths, append entries to <see cref="Paths"/>.
/// </summary>
public static class NonEditableNodes
{
    public static readonly HashSet<string> Paths = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Profile → Shipper references ─────────────────────────────────────
        // The actual shipper data lives at Company → Shippers.
        "Profile.Shippers",          // container
        "Profile.Shipper",           // individual items

        // ── Profile → Carrier references ─────────────────────────────────────
        // The actual carrier data lives at Company → Carriers.
        "Profile.Carriers",          // container
        "Profile.Carrier",           // individual items

        // ── Profile → singular configuration references ──────────────────────
        // These reference entities managed at Company level.
        "Profile.DocumentConfiguration",
        "Profile.PrinterConfiguration",
        "Profile.ScaleConfiguration",
    };

    /// <summary>
    /// Returns <c>true</c> when <paramref name="nodePath"/> is at or below a configured path.
    /// Supports both full-path and suffix-path matching — see class summary.
    /// </summary>
    public static bool IsNonEditable(string nodePath)
    {
        var segments = nodePath.Split('.');

        foreach (var configured in Paths)
        {
            var configuredDepth = configured.Count(c => c == '.') + 1;

            // Walk every prefix of nodePath that is at least as long as the configured path.
            // If that prefix is a segment-match for the configured path, the node (or one of
            // its ancestors) is non-editable, so this node is non-editable too.
            for (int take = configuredDepth; take <= segments.Length; take++)
            {
                var prefix = string.Join(".", segments, 0, take);
                if (SegmentMatch(prefix, configured))
                    return true;
            }
        }

        return false;
    }

    // Returns true when 'path' equals 'configured' (full-path match)
    // or when 'path' ends with '.<configured>' (suffix match).
    private static bool SegmentMatch(string path, string configured) =>
        path.Equals(configured, StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("." + configured, StringComparison.OrdinalIgnoreCase);
}
