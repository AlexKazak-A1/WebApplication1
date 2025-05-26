using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO.Node;

/// <summary>
/// Описывает колличество ОЗУ в кластере Proxmox
/// </summary>
public class NodeMemoryDTO
{
    [JsonProperty("used")]
    public long Used { get; set; } = 0;

    [JsonProperty("free")]
    public long Free { get; set; } = 0;

    [JsonProperty("total")]
    public long Total { get; set; } = 0;
}
