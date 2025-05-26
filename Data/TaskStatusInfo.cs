namespace WebApplication1.Data;

/// <summary>
/// Описывает информацию состояния задачи
/// </summary>
public class TaskStatusInfo
{
    /// <summary>
    /// ID задачи 
    /// </summary>
    public Guid TaskId { get; set; }
    
    /// <summary>
    /// Текущий сратус задачи
    /// </summary>
    public string Status { get; set; } = "InProgress"; // "InProgress", "Completed", "Failed"

    /// <summary>
    /// Результат исполнения задачи
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Сообщение с описанием ошибки, если она возникла
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Последнее время обновления задачи
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
