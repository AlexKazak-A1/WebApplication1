using Newtonsoft.Json;
using WebApplication1.Data.ProxmoxDTO;

namespace WebApplication1.Models;

public class ProxmoxDefaultConfig
{
    [JsonProperty("vmTemplateName")]
    public string VmTemplateName { get; set; } = string.Empty;

    [JsonProperty("controlPlaneAmount")]
    public int ControlPlaneAmount { get; set; } = 1;

    [JsonProperty("etcdProvisionRange")]
    public List<string> EtcdProvisionRange { get; set; } = new List<string>();

    [JsonProperty("etcdConfig")]
    public TemplateParams EtcdConfig { get; set; } = new TemplateParams { CPU = "2", RAM = "4", HDD = "12" };

    [JsonProperty("workerProvisionRange")]
    public List<string> WorkerProvisionRange { get; set; } = new List<string>();

    [JsonProperty("selectedStorage")]
    public string SelectedStorage { get; set; } = string.Empty;

    [JsonProperty("vlanTest")]
    public int VlanTest { get; set; } = default;

    [JsonProperty("vlanProd")]
    public int VlanProd { get; set; } = default;


}
