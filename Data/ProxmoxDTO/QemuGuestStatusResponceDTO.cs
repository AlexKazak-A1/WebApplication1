using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class QemuGuestStatusResponceDTO
{
    [JsonProperty("exitcode")]
    public int ExitCode { get; set; }

    [JsonProperty("exited")]
    public bool Exited { get; set; }

    [JsonProperty("out-data")]
    public string OutPut { get; set; }
}
