using Newtonsoft.Json;

namespace WebApplication1.Data.WEB;

/// <summary>
/// Describes what VM should be connected
/// </summary>
public class ConnectVmToRancherDTO
{
    /// <summary>
    /// Connection string to Rancher cluster
    /// </summary>
    [JsonProperty("connectionString")]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Unique Id of Proxmox VM
    /// </summary>
    [JsonProperty("vmsId")]
    public List<int> VMsId { get; set; }

    /// <summary>
    /// Proxmox Id From DB
    /// </summary>
    [JsonProperty("proxmoxId")]
    public string ProxmoxId { get; set; }
}
