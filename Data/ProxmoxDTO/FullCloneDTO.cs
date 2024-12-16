using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class FullCloneDTO
{
    /// <summary>
    /// VMID for the clone
    /// </summary>
    [JsonProperty("newid")]
    public int NewId { get; set; }

    /// <summary>
    /// The cluster node name
    /// </summary>
    [JsonProperty("node")]
    public string Node { get; set; }

    /// <summary>
    /// The (unique) ID of the VM
    /// </summary>
    [JsonProperty("vmid")]
    public int VMId { get; set; }

    /// <summary>
    /// Sets a name for the new VM
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// Create a full copy of all disks.
    /// Better to enable for new VM`s
    /// </summary>
    [JsonProperty("full")]
    public bool Full { get; set; }
}
