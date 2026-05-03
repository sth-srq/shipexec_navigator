namespace ShipExecAgent.Shared.Models;

public class DiffResult
{
    public List<VarianceInfo> Variances { get; set; } = [];
    public List<RequestInfo> Requests { get; set; } = [];

    public bool HasChanges => Variances.Count > 0;
}
