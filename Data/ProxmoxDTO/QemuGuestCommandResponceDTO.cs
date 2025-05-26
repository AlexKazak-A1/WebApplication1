using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Описывает ответ от Proxmox по исполняемой команде
/// </summary>
public class QemuGuestCommandResponceDTO
{
    /// <summary>
    /// Уникальный номер для отслеживания состояния выполняемой команды
    /// </summary>
    [JsonProperty("pid")]
    public int? Pid { get; set; }
}
