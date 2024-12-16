using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

public class TemplateParams
{
    [JsonProperty("cpu")]
    public string CPU {  get; set; }

    [JsonProperty("ram")]
    public string RAM { get; set; }

    [JsonProperty("hdd")]
    public string HDD { get; set; }
}
