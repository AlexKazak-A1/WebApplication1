using Newtonsoft.Json;

namespace WebApplication1.Data.RancherDTO;

public class StatusInfo
{
    [JsonProperty("observedGeneration")]
    public int ObservedGeneration { get; set; }

    [JsonProperty("clusterName")]
    public string ClusterName { get; set; }
}
