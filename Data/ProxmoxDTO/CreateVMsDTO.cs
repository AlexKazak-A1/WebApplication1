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

    [JsonProperty("vmTemplateName")]
    public string VMTemplateName { get; set; }

    [JsonProperty("rancherId")]
    public int RancherId { get; set; }

    [JsonProperty("clusterName")]
    public string ClusterName { get; set; }

    [JsonProperty("vmStartIndex")]
    public int VMStartIndex { get; set; }

    [JsonProperty("vmPrefix")]
    public string VMPrefix { get; set; } = string.Empty;

    [JsonProperty("vmConfig")]
    public TemplateParams VMConfig { get; set; }

    [JsonProperty("etcdConfig")]
    public TemplateParams etcdConfig { get; set; }

    [JsonProperty("provisionSchema")]
    public Dictionary<string,List<string>>? ProvisionSchema { get; set; }

    [JsonProperty("etcdProvisionRange")]
    public List<string> ETCDProvisionRange { get; set; }

    [JsonProperty("workerProvisionRange")]
    public List<string> WorkerProvisionRange { get; set; }

    [JsonProperty("selectedStorage")]
    public List<string> SelectedStorage { get; set; }
}