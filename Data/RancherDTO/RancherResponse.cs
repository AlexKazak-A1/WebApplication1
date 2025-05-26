using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace WebApplication1.Data.RancherDTO;

/// <summary>
/// Описывает ответ от RAncher кластера на отправляемые запросы
/// </summary>
public class RancherResponse
{
    public string Type { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }

    [JsonPropertyName("data")]
    public object Data { get; set; }

    [JsonProperty("error")]
    public object Error { get; set; }

}
