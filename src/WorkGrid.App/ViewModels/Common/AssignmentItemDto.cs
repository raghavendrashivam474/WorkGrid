namespace WorkGrid.App.ViewModels.Common;

public sealed class AssignmentHistoryItem
{
    public Guid AssignmentId { get; set; }
    public Guid ReferenceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? ReturnedAt { get; set; }
    public bool IsActive => !ReturnedAt.HasValue;
    public string StatusDisplay => IsActive ? "Active" : "Returned";
}
