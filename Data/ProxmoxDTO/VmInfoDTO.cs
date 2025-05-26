using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Описывает общую информацию о VM 
/// </summary>
public class VmInfoDTO
{
    /// <summary>
    /// Amount currently user CPU
    /// </summary>
    [JsonProperty("cpu")]
    public double Cpu { get; set; }

    /// <summary>
    /// Unique ID of VM according current Proxmox cluster
    /// </summary>
    [JsonProperty("vmid")]
    public int VmId { get; set; }

    /// <summary>
    /// Total Amount of CPU given to VM
    /// </summary>
    [JsonProperty("cpus")]
    public int CPUS { get; set; }

    /// <summary>
    /// Amount of input net trafic
    /// </summary>
    [JsonProperty("netin")]
    public long NetIn { get; set; }

    /// <summary>
    /// Amount of reading on disk
    /// </summary>
    [JsonProperty("diskread")]
    public double DiskRead { get; set; }

    /// <summary>
    /// Max amount of memory in HDD
    /// </summary>
    [JsonProperty("maxdisk")]
    public long MaxDisk { get; set; }

    /// <summary>
    /// Max amount of RAM
    /// </summary>
    [JsonProperty("maxmem")]
    public long MaxMem { get; set; }

    /// <summary>
    /// Amount of working time since last start
    /// </summary>
    [JsonProperty("uptime")]
    public long Uptime { get; set; }

    /// <summary>
    /// Describes what serial is used
    /// </summary>
    [JsonProperty("serial")]
    public int Serial { get; set; }

    /// <summary>
    /// Amount of writing to hdd 
    /// </summary>
    [JsonProperty("diskwrite")]
    public double DiskWrite { get; set; }

    /// <summary>
    /// Amount of allocated RAM
    /// </summary>
    [JsonProperty("mem")]
    public long Mem { get; set; }

    /// <summary>
    /// Amount of outgoing net traffic
    /// </summary>
    [JsonProperty("netout")]
    public long NetOut { get; set; }

    /// <summary>
    /// Name of VM in Proxmox cluster
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// Status of VM
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; }

    /// <summary>
    /// Determines if this VM is template or not
    /// </summary>
    [JsonProperty("template")]
    public int Template { get; set; }
}
