using WebApplication1.Data;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;

namespace WebApplication1.Data.Jira;

/// <summary>
/// Специальный DTO, описывающий взаимодействие с Jira при отправке запроса на создание кластера
/// </summary>
public class JiraCreateClusterRequestDTO
{
    /// <summary>
    /// Уникальное имя для Proxmox 
    /// </summary>
    public string UniqueProxmoxName { get; set; } = string.Empty;

    /// <summary>
    /// Иникальное имя для Rancher
    /// </summary>
    public string UniqueRancherName { get; set; } = string.Empty;

    /// <summary>
    /// Описывает выбранное окружение для развёртывания (PROD | TEST)
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Задаётся колличество рабочих узлов для развёртывания кластера
    /// </summary>
    public int WorkerAmount { get; set; } = default;

    /// <summary>
    /// Задаёт параметры для рабочих узлов
    /// </summary>
    public TemplateParams ParamsPerWorker { get; set; } = default;

    /// <summary>
    /// Задаёт имя Rancher кластера
    /// </summary>
    public string ClusterName { get; set; } = default;    
}
