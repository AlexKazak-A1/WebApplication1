using System.Text.Json.Serialization;

namespace WebApplication1.Data.ProxmoxDTO;

public class ProxmoxQemuDTO
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("serial")]
    public bool Serial { get; set; }

    [JsonPropertyName("template")]
    public bool Template { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("vmid")]
    public int VmId { get; set; }
}
