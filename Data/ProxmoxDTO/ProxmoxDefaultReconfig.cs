using Newtonsoft.Json;
using WebApplication1.Models;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Описывает все изменяемые параметры в Proxmox 
/// </summary>
public class ProxmoxDefaultReconfig
{
    /// <summary>
    /// ID записи в БД
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// новое уникальное имя для Proxmox
    /// </summary>
    [JsonProperty("uniqueProxmoxName")]
    public string UniqueProxmoxName { get; set; }

    /// <summary>
    /// Обновлённая конфигурация по умолчанию
    /// </summary>
    [JsonProperty("defaultConfig")]
    public ProxmoxDefaultConfig DefaultConfig { get; set; }
}
