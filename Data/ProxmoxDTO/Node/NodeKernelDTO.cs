using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO.Node;

/// <summary>
/// Описывает ядро Proxmox на котором он работает
/// </summary>
public class NodeKernelDTO
{
    [JsonProperty("relese")]
    public string Release { get; set; } = string.Empty;

    [JsonProperty("sysname")]
    public string SysName { get; set; } = string.Empty;

    [JsonProperty("machine")]
    public string Machine { get; set; } = string.Empty;

    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;
}
