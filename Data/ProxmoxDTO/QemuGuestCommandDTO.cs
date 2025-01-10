using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class QemuGuestCommandDTO
{
    [JsonProperty("command")]
    public string Command { get; set; }

    [JsonProperty("input-data")]
    public string InputData { get; set; }
}
