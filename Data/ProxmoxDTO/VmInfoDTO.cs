using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class VmInfoDTO
{
    /// <summary>
    /// Amount currently user CPU
    /// </summary>
    [JsonProperty("cpu")]
    public double Cpu { get; set; }

    [JsonProperty("vmid")]
    public int VmId { get; set; }

    /// <summary>
    /// Total Amount of CPU given to VM
    /// </summary>
    [JsonProperty("cpus")]
    public int CPUS { get; set; }

    [JsonProperty("netin")]
    public long NetIn { get; set; }

    [JsonProperty("diskread")]
    public double DiskRead { get; set; }

    [JsonProperty("maxdisk")]
    public long MaxDisk { get; set; }

    [JsonProperty("maxmem")]
    public long MaxMem { get; set; }

    [JsonProperty("uptime")]
    public long Uptime { get; set; }

    [JsonProperty("serial")]
    public int Serial { get; set; }

    [JsonProperty("diskwrite")]
    public double DiskWrite { get; set; }

    [JsonProperty("mem")]
    public long Mem { get; set; }

    [JsonProperty("netout")]
    public long NetOut { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("template")]
    public int Template { get; set; }
}
