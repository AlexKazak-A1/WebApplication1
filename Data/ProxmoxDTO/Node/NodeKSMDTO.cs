using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO.Node;

public class NodeKSMDTO
{
    [JsonProperty("shared")]
    public double Shared { get; set; } = 0.0;
}
