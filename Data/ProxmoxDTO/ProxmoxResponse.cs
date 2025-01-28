using Newtonsoft.Json;
using System.Text.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class ProxmoxResponse
{    
    [JsonProperty("data")]
    public object Data { get; set; }

    [JsonProperty("error")]
    public object Error { get; set; }

    [JsonProperty("err-data")]
    public string? ErrorData { get; set; }
}
