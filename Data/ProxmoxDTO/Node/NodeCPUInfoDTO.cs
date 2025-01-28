using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO.Node;

public class NodeCPUInfoDTO
{
    [JsonProperty("coers")]
    public int Cores { get; set; } = 0;

    [JsonProperty("hvm")]
    public string HVM { get; set; } = string.Empty;

    [JsonProperty("sockets")]
    public int Sockets { get; set; } = 0;

    [JsonProperty("cpus")]
    public int CPUs { get; set; } = 0;

    [JsonProperty("flags")]
    public string Flags { get; set; } = string.Empty;

    [JsonProperty("model")]
    public string Model { get; set; } = string.Empty;

    [JsonProperty("mhz")]
    public string MHZ { get; set; } = string.Empty;

    [JsonProperty("user_hz")]
    public int User_hz { get; set; } = 0;
}
