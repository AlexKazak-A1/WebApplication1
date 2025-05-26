using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Описывает состояние задачи откравленной на исполнение
/// </summary>
public class TaskStatusDTO
{
    /// <summary>
    /// статус задачи/команды
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; }

    /// <summary>
    /// статус после завершения
    /// </summary>
    [JsonProperty("exitstatus")]
    public string? ExitStatus { get; set; } = null;
}
