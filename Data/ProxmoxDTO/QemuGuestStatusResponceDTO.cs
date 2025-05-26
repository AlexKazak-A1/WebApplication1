using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Описывает состояние активности агента, исполняющего команды
/// </summary>
public class QemuGuestStatusResponceDTO
{
    /// <summary>
    /// Код состояния
    /// </summary>
    [JsonProperty("exitcode")]
    public int ExitCode { get; set; }

    /// <summary>
    /// Отображает завершился ли процесс
    /// </summary>
    [JsonProperty("exited")]
    public bool Exited { get; set; }

    /// <summary>
    /// Дополнительняи информация
    /// </summary>
    [JsonProperty("out-data")]
    public string OutPut { get; set; }
}
