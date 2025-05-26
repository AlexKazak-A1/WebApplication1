using Newtonsoft.Json;

namespace WebApplication1.Data.RancherDTO;

/// <summary>
/// Описывает данные изменяемые в записи БД, связанной с Rancher
/// </summary>
public class RancherReconfigDTO
{
    /// <summary>
    /// ID в базе данных для поиска записи
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// новое уникальное имя Rancher кластера 
    /// </summary>
    [JsonProperty("uniqueRancherName")]
    public string UniqueRancherName { get; set; }
}
