using Newtonsoft.Json;
using WebApplication1.Data.ProxmoxDTO.Node;

namespace WebApplication1.Data.ProxmoxDTO;

public class ProxmoxNodeStatusDTO
{
    [JsonProperty("memory")]
    public NodeMemoryDTO Memory { get; set; }

    [JsonProperty("cpuinfo")]
    public NodeCPUInfoDTO CPUInfo { get; set; }

    [JsonProperty("loadavg")]
    public string[] LoadAVG { get; set; }

    [JsonProperty("ksm")]
    public NodeKSMDTO Ksm { get; set; }

    [JsonProperty("current-kernel")]
    public NodeKernelDTO Current_kernel { get; set; }

    [JsonProperty("cpu")]
    public double CPU { get; set; } = 0;

    [JsonProperty("boot-info")]
    public NodeBootInfoDTO Boot_Info { get; set; }

    [JsonProperty("wait")]
    public double Wait { get; set; } = 0;

    [JsonProperty("rootfs")]
    public NodeRootFSDTO RootFS { get; set; }

    [JsonProperty("pveversion")]
    public string PVEVersion { get; set; } = string.Empty;

    [JsonProperty("uptime")]
    public long UpTime { get; set; } = 0;

    [JsonProperty("swap")]
    public NodeSwapDTO Swap { get; set; }

    [JsonProperty("idle")]
    public double Idle { get; set; } = 0;

    [JsonProperty("kversion")]
    public string Kversion { get; set; } = string.Empty;

}
