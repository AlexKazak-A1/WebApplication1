using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class QemuStatusDTO
{
    [JsonProperty("status")]
    public string Status { get; set; }
}
