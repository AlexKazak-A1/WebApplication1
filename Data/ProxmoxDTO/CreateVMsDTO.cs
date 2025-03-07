using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Info about all type of VMs that must be created in Proxmox
/// </summary>
public class CreateVMsDTO
{
    /// <summary>
    /// Id of Proxmox from DB
    /// </summary>
    [JsonProperty("proxmoxId")]
    public int ProxmoxId { get; set; }

    /// <summary>
    /// Amount of Control Planes that must be created
    /// </summary>
    [JsonProperty("etcdAndControlPlaneAmount")]
    public int EtcdAndControlPlaneAmount { get; set; }

    /// <summary>
    /// Amount of Workers that must be created
    /// </summary>
    [JsonProperty("workerAmount")]
    public int WorkerAmount { get; set; }

    /// <summary>
    /// Name of specified Proxmox template
    /// </summary>
    [JsonProperty("vmTemplateName")]
    public string VMTemplateName { get; set; }

    /// <summary>
    /// Id of Rancher from DB
    /// </summary>
    [JsonProperty("rancherId")]
    public int RancherId { get; set; }

    /// <summary>
    /// New Rancher Cluster name
    /// </summary>
    [JsonProperty("clusterName")]
    public string ClusterName { get; set; }

    /// <summary>
    /// Index from with creation of VMs will be started
    /// </summary>
    [JsonProperty("vmStartIndex")]
    public int VMStartIndex { get; set; }

    /// <summary>
    /// The same as Rancher cluster name
    /// </summary>
    [JsonProperty("vmPrefix")]
    public string VMPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Contains Params for workers
    /// </summary>
    [JsonProperty("vmConfig")]
    public TemplateParams VMConfig { get; set; }

    /// <summary>
    /// Contains params for Control Planes
    /// </summary>
    [JsonProperty("etcdConfig")]
    public TemplateParams etcdConfig { get; set; }

    /// <summary>
    /// Contains info about what vms and on what Proxmox Host need to be deployed
    /// </summary>
    [JsonProperty("provisionSchema")]
    public Dictionary<string,List<string>>? ProvisionSchema { get; set; }

    /// <summary>
    /// Range of hosts for Control Planes
    /// </summary>
    [JsonProperty("etcdProvisionRange")]
    public List<string> ETCDProvisionRange { get; set; }

    /// <summary>
    /// Range of hosts for workers
    /// </summary>
    [JsonProperty("workerProvisionRange")]
    public List<string> WorkerProvisionRange { get; set; }

    /// <summary>
    /// Range of storages for provision VMs (will be selected with max available size).
    /// </summary>
    [JsonProperty("selectedStorage")]
    public List<string> SelectedStorage { get; set; }
}