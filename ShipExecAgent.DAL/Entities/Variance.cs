namespace ShipExecAgent.DAL.Entities;

public class Variance
{
    public long      Id          { get; set; }
    public Guid?     BatchId     { get; set; }
    public Guid?     CompanyId   { get; set; }
    public Guid?     UserId      { get; set; }
    public string?   NewEntity      { get; set; }
    public string?   OriginalEntity { get; set; }
    public string?   VarianceData   { get; set; }
    public string?   Comments    { get; set; }
    public string?   Endpoint    { get; set; }
    public DateTime  CreatedOn   { get; set; }
    public bool      IsActive    { get; set; }
}
