using Newtonsoft.Json;
using WebApplication1.Data.ProxmoxDTO;

namespace WebApplication1.Models;

public class ProxmoxDefaultConfig
{
    [JsonProperty("vmTemplateName")]
    public string VmTemplateName { get; set; } = string.Empty; // имя шаблона для развёртывания из Proxmox

    [JsonProperty("controlPlaneAmount")]
    public int ControlPlaneAmount { get; set; } = 1; // колличество управляющих узлов

    [JsonProperty("etcdProvisionRange")]
    public List<string> EtcdProvisionRange { get; set; } = new List<string>(); // диапазон хостов Porxmox на которых будет производиться развёртывание управляющих узлов

    [JsonProperty("etcdConfig")]
    public TemplateParams EtcdConfig { get; set; } = new TemplateParams { CPU = "2", RAM = "4", HDD = "12" }; // конфиг, используемый для выставления параметров для управляющих узлов

    [JsonProperty("workerProvisionRange")]
    public List<string> WorkerProvisionRange { get; set; } = new List<string>(); // диапазон хостов Porxmox на которых будет производиться развёртывание рабочих узлов

    [JsonProperty("selectedStorage")]
    public string SelectedStorage { get; set; } = string.Empty; // хранилище, выбранное для размещения VM при клонировании

    [JsonProperty("vlanTest")]
    public int VlanTest { get; set; } = default; // Номер для тестового VLAN

    [JsonProperty("vlanProd")]
    public int VlanProd { get; set; } = default; // Номер для продуктивного VLAN


}
