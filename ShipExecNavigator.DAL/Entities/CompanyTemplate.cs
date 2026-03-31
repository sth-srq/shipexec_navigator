namespace ShipExecNavigator.DAL.Entities;

public class CompanyTemplate
{
    public long      Id           { get; set; }
    public Guid      CompanyId    { get; set; }
    public int       TemplateId   { get; set; }
    public string    CompanyName  { get; set; } = string.Empty;
    public string    TemplateName { get; set; } = string.Empty;
    public string    TemplateType { get; set; } = string.Empty;
    public string?   TemplateData { get; set; }
    public string?   EndpointUrl  { get; set; }
    public DateTime  FetchedOn    { get; set; }
}
