using Newtonsoft.Json;

namespace WebApplication1.Data.RancherDTO;

public class ClusterStatusDTO
{
    [JsonProperty("status")]
    public StatusInfo Status { get; set; }

    [JsonProperty("metadata")]
    public RancherMetadata Metada { get; set; }
}
