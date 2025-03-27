namespace WebApplication1.Data;

public class TaskStatusInfo
{
    public Guid TaskId { get; set; }
    public string Status { get; set; } = "InProgress"; // "InProgress", "Completed", "Failed"
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
