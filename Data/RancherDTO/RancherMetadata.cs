using Newtonsoft.Json;

namespace WebApplication1.Data.RancherDTO;

public class RancherMetadata
{
    [JsonProperty("name")]
    public string Name { get; set; }
}
