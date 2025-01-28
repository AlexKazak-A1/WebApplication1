using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO.Node;

public class NodeBootInfoDTO
{
    [JsonProperty("mode")]
    public string Mode { get; set; } = string.Empty;
}
