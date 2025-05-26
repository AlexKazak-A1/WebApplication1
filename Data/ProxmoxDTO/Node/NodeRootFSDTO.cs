using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO.Node;

/// <summary>
/// Описывает корневую файловую систему на хосте Proxmox
/// </summary>
public class NodeRootFSDTO
{
    [JsonProperty("free")]
    public long Free { get; set; } = 0;

    [JsonProperty("total")]
    public long Total { get; set; } = 0;

    [JsonProperty("used")]
    public long Used { get; set; } = 0;

    [JsonProperty("avail")]
    public long Avail { get; set; } = 0;
}
