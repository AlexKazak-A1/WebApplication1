using Newtonsoft.Json;

namespace WebApplication1.Data.RancherDTO;

/// <summary>
/// Описывает состояние кластера Rancher
/// </summary>
public class ClusterStatusDTO
{
    /// <summary>
    /// Текущий статус кластера
    /// </summary>
    [JsonProperty("status")]
    public StatusInfo Status { get; set; }

    /// <summary>
    /// Метаданные кластера
    /// </summary>
    [JsonProperty("metadata")]
    public RancherMetadata Metada { get; set; }
}
