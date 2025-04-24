using Newtonsoft.Json;
using WebApplication1.Models;

namespace WebApplication1.Data.WEB;

public class ProxmoxDefaultReconfig
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("uniqueProxmoxName")]
    public string UniqueProxmoxName { get; set; }

    [JsonProperty("defaultConfig")]
    public ProxmoxDefaultConfig DefaultConfig { get; set; }
}
