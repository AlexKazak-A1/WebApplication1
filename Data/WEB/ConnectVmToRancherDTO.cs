using Newtonsoft.Json;

namespace WebApplication1.Data.WEB;

public class ConnectVmToRancherDTO
{
    [JsonProperty("connectionString")]
    public string ConnectionString { get; set; }

    [JsonProperty("vmsId")]
    public List<int> VMsId { get; set; }

    [JsonProperty("proxmoxId")]
    public string ProxmoxId { get; set; }
}
