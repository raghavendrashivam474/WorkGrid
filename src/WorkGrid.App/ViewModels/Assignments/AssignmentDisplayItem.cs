namespace WorkGrid.App.ViewModels.Assignments;

public sealed class AssignmentDisplayItem
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public Guid AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive => Status == "Active";
}
