using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Contains Info for Proxmox VMs
/// </summary>
public class TemplateParams
{
    /// <summary>
    /// Amount of CPUs
    /// </summary>
    [JsonProperty("cpu")]
    public string CPU {  get; set; }

    /// <summary>
    /// Amount of RAM 
    /// </summary>
    [JsonProperty("ram")]
    public string RAM { get; set; }

    /// <summary>
    /// Amount of HDD 
    /// </summary>
    [JsonProperty("hdd")]
    public string HDD { get; set; }
}
