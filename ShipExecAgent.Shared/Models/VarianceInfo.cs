namespace ShipExecAgent.Shared.Models;

public class VarianceInfo
{
    public string EntityName { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string OriginalXML { get; set; }
    public string NewXML { get; set; }
    /// <summary>
    /// Set when this variance belongs to a parent entity (e.g. "Site: Chicago").
    /// Empty for top-level entity variances.
    /// </summary>
    public string ParentContext { get; set; } = string.Empty;

    /// <summary>
    /// Index into the source <c>_lastVariances</c> list — used to map display items
    /// back to the BL Variance that drives the apply request.
    /// Child variances that share a parent BL variance carry the same index.
    /// </summary>
    public int VarianceIndex { get; set; }
}
