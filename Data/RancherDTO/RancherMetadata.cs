using Newtonsoft.Json;

namespace WebApplication1.Data.RancherDTO;

/// <summary>
/// Описывает метаданные кластера
/// </summary>
public class RancherMetadata
{
    [JsonProperty("name")]
    public string Name { get; set; }
}
