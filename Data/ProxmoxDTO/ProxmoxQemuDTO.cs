using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class ProxmoxQemuDTO
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("serial")]
    public bool Serial { get; set; }

    [JsonProperty("template")]
    public bool Template { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("vmid")]
    public int VmId { get; set; }

    [JsonProperty("maxmem")]
    public ulong MaxMem { get; set; } = 0;

    [JsonProperty("cpus")]
    public int CPUs { get; set; } = 0;

    [JsonProperty("maxdisk")]
    public ulong MaxDisk { get; set; } = 0;

}
