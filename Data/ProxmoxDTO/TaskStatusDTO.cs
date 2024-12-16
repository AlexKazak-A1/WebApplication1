using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class TaskStatusDTO
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("exitstatus")]
    public string? ExitStatus { get; set; } = null;
}
