using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Описывает структуру командя для исполнения внутри VM, передоваемую при помощи внутренних мехонизмов Proxmox
/// </summary>
public class QemuGuestCommandDTO
{
    /// <summary>
    /// Основная команда передаваемая для исполнения в VM
    /// </summary>
    [JsonProperty("command")]
    public string Command { get; set; }

    /// <summary>
    /// ПАраметры, передаваемые для исполняемой команды
    /// </summary>
    [JsonProperty("input-data")]
    public string InputData { get; set; }
}
