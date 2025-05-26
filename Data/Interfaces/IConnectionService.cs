using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Data.Interfaces;

/// <summary>
/// Описывает поведение Проверки подключенияк ресурсам
/// </summary>
public interface IConnectionService 
{

    /// <summary>
    /// Проверка подключения к Rancher
    /// </summary>
    /// <param name="model">Модель для описания параметров подключения к Rancher</param>
    /// <returns>Возвращает JSon со статусом проверки подключения</returns>
    public Task<JsonResult> CheckRancherUrl([FromBody] UrlRancherCheckModel model);

    /// <summary>
    /// Проверка подключения к Proxmox
    /// </summary>
    /// <param name="model">Модель для описания параметров подключения к Proxmox</param>
    /// <returns>Возвращает JSon со статусом проверки подключения</returns>
    public Task<JsonResult> CheckProxmoxUrl([FromBody] UrlProxmoxCheckModel model);
}
