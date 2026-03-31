namespace ShipExecNavigator.Shared.Models;

public class CompanyInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(Symbol)
        ? Name
        : $"{Name} ({Symbol})";
}
