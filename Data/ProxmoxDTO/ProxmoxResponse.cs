using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace WebApplication1.Data.ProxmoxDTO;

public class ProxmoxResponse
{
    [JsonPropertyName("data")]
    public object Data { get; set; }

    [JsonProperty("error")]
    public object Error { get; set; }
}
