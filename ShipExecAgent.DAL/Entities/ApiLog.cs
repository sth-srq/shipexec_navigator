namespace ShipExecAgent.DAL.Entities;

public class ApiLog
{
    public long      Id           { get; set; }
    public DateTime  OccurredOn   { get; set; }
    public string?   Category     { get; set; }     // e.g. "ShipExecApi", "AzureOpenAI", "OpenAI"
    public string?   Operation    { get; set; }     // e.g. "GetCompanies", "LoadCategory", "ChatCompletion"
    public string?   RequestData  { get; set; }     // JSON summary of what was sent
    public string?   ResponseData { get; set; }     // JSON summary of what was received
    public long?     DurationMs   { get; set; }
    public bool      IsSuccess    { get; set; }
    public string?   ErrorMessage { get; set; }
    public Guid?     CompanyId    { get; set; }
    public string?   AdditionalInfo { get; set; }  // free-form JSON for extra context
}
