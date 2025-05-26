using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

/// <summary>
/// Describes model of Proxmox for DB
/// </summary>
public class ProxmoxModel
{
    /// <summary>
    /// Unique Id for DB record
    /// </summary>
    [Key]
    public int Id { get; set; } // уникальный Идентификатор в БД

    /// <summary>
    /// URL for Proxmox cluster/host
    /// </summary>
    public string ProxmoxURL {  get; set; } // URL для подключения в PRoxmox, включает и порт

    /// <summary>
    /// Acsess token in format PVEAPIToken=User!TokenID=secret
    /// </summary>
    public string ProxmoxToken { get; set; }  // Токен для подключения к PRoxmox(составной токен)

    /// <summary>
    /// Unique name that must be the same as in CMDB & comes from Jira
    /// </summary>
    public string ProxmoxUniqueName { get; set; } // уникальное имя испрользуемое, как идентификатор при взаимодействии с Jira

    /// <summary>
    /// Describes default params for Proxmox Cluster/Host
    /// </summary>
    public ProxmoxDefaultConfig? DefaultConfig { get; set; } // конфиг по умолчанию, где выставляются параметры для конкретного Proxmox
}
