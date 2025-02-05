using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class QemuGuestCommandResponceDTO
{
    [JsonProperty("pid")]
    public int? Pid { get; set; }
}
