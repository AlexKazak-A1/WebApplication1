using Newtonsoft.Json;

namespace WebApplication1.Data.RancherDTO;

public class RancherReconfigDTO
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("uniqueRancherName")]
    public string UniqueRancherName { get; set; }
}
