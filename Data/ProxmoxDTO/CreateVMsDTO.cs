using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class CreateVMsDTO
{
    [JsonProperty("proxmoxId")]
    public int ProxmoxId { get; set; }

    [JsonProperty("etcdAndControlPlaneAmount")]
    public int EtcdAndControlPlaneAmount { get; set; }

    [JsonProperty("workerAmount")]
    public int WorkerAmount { get; set; }

    [JsonProperty("vmTemplateId")]
    public int VMTemplateId { get; set; }

    [JsonProperty("rancherId")]
    public int RancherId { get; set; }

    [JsonProperty("clusterName")]
    public string ClusterName { get; set; }

    [JsonProperty("vmStartIndex")]
    public int VMStartIndex { get; set; }

    [JsonProperty("vmPrefix")]
    public string VMPrefix { get; set; } = string.Empty;
}