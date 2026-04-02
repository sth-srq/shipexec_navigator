namespace ShipExecNavigator.Shared.Models;

public class CsvUserCreateResult
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
}
