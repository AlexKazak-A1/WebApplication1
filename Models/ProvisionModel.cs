using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Models;

public class ProvisionModel :PageModel
{

    public ProxmoxModel Proxmox { get; set; } // модель описывающая Proxmox в БД

    public RancherModel Rancher { get; set; } // модель описывающая Rancher в БД

    public int NumberOfETCDAndControlPlane { get; set; } = 3; // колличество управляющих узлов

    public int numberOfWorker { get; set; } // колличество рабочих узлов

    public void OnGet()
    {
        // Читаем данные из cookies
        Proxmox.ProxmoxURL = Request.Cookies["proxmoxUrl"];
        Proxmox.ProxmoxToken = Request.Cookies["proxmoxToken"];
        Rancher.RancherURL = Request.Cookies["rancherUrl"];
        Rancher.RancherToken = Request.Cookies["rancherToken"];
    }
}
