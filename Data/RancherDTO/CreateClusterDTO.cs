using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WebApplication1.Data.RancherDTO
{
    public class CreateClusterDTO
    {
        [JsonProperty("rancherId")]
        public string RancherId { get; set; }

        [JsonProperty("clusterName")]
        public string ClusterName { get; set; }
    }
}
