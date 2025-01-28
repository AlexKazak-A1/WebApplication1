using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO.Node;

public class NodeSwapDTO
{
    [JsonProperty("used")]
    public long Used { get; set; } = 0;

    [JsonProperty("free")]
    public long Free { get; set; } = 0;

    [JsonProperty("total")]
    public long Total { get; set; } = 0;
}
