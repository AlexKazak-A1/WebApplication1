using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Models;

public class ProvisionModel :PageModel
{

    public ProxmoxModel Proxmox { get; set; }

    public RancherModel Rancher { get; set; }

    public int NumberOfETCDAndControlPlane { get; set; } = 3;

    public int numberOfWorker { get; set; }

    public void OnGet()
    {
        // Читаем данные из cookies
        Proxmox.ProxmoxURL = Request.Cookies["proxmoxUrl"];
        Proxmox.ProxmoxToken = Request.Cookies["proxmoxToken"];
        Rancher.RancherURL = Request.Cookies["rancherUrl"];
        Rancher.RancherToken = Request.Cookies["rancherToken"];
    }
}
