using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO.Node;

public class NodeBootInfoDTO
{
    /// <summary>
    /// Описывает тип загрузки для хоста Proxmox
    /// </summary>
    [JsonProperty("mode")]
    public string Mode { get; set; } = string.Empty;
}
