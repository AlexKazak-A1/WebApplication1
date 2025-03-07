using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WebApplication1.Data.RancherDTO
{
    /// <summary>
    /// Info for new Rancher Cluster
    /// </summary>
    public class CreateClusterDTO
    {
        /// <summary>
        /// Id of Rancher from DB
        /// </summary>
        [JsonProperty("rancherId")]
        public string RancherId { get; set; }
        
        /// <summary>
        /// New Rancher Cluster Name
        /// </summary>
        [JsonProperty("clusterName")]
        public string ClusterName { get; set; }
    }
}
