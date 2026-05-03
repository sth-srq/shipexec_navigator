using ShipExecAgent.BusinessLogic.EntityComparison;

namespace ShipExecAgent.Services;

/// <summary>
/// Scoped service that temporarily holds shipper-import variances when
/// the user clicks "Add to Variance List" on the Tools page.
/// The XmlViewer page consumes (and clears) them after connecting.
/// </summary>
public class PendingImportService
{
    private readonly List<Variance> _pendingShipperVariances = [];

    /// <summary>True when there are variances waiting to be consumed.</summary>
    public bool HasPendingShipperVariances => _pendingShipperVariances.Count > 0;

    /// <summary>Category to highlight after navigation (e.g. "Shippers").</summary>
    public string? HighlightCategory { get; set; }

    public void SetPendingShipperVariances(List<Variance> variances, string? highlightCategory = "Shippers")
    {
        _pendingShipperVariances.Clear();
        _pendingShipperVariances.AddRange(variances);
        HighlightCategory = highlightCategory;
    }

    public List<Variance> ConsumePendingShipperVariances()
    {
        var result = new List<Variance>(_pendingShipperVariances);
        _pendingShipperVariances.Clear();
        HighlightCategory = null;
        return result;
    }
}
