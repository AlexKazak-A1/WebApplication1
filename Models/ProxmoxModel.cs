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
    public int Id { get; set; }

    /// <summary>
    /// URL for Proxmox cluster/host
    /// </summary>
    public string ProxmoxURL {  get; set; }

    /// <summary>
    /// Acsess token in format PVEAPIToken=User!TokenID=secret
    /// </summary>
    public string ProxmoxToken { get; set; }  
}
