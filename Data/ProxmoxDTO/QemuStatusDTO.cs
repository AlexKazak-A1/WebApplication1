using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// описывает статус VM
/// </summary>
public class QemuStatusDTO
{
    [JsonProperty("status")]
    public string Status { get; set; }
}
