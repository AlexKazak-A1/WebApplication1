using Newtonsoft.Json;
using System.Text.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Описывает структуру частичную общего отвера от Proxmox
/// </summary>
public class ProxmoxResponse
{    
    /// <summary>
    /// Область с полезными данными
    /// </summary>
    [JsonProperty("data")]
    public object Data { get; set; }

    /// <summary>
    /// Область ошибок при их возникновении
    /// </summary>
    [JsonProperty("error")]
    public object Error { get; set; }

    /// <summary>
    /// Область, дополняющая область с ошибками. Доп. информация к возникшей ошибке
    /// </summary>
    [JsonProperty("err-data")]
    public string? ErrorData { get; set; }
}
