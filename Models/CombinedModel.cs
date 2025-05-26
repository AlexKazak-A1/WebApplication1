using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Models;

public class CombinedModel: PageModel // общая модель для проверки подключений
{
    public UrlRancherCheckModel RancherCheckModel { get; set; } // модель описывающая подключение к Rancher
    public UrlProxmoxCheckModel ProxmoxCheckModel { get; set; } // модель описывающая подключение к Proxmox
    public UrlCheckResponse UrlCheckResponse { get; set; } // ответ на попытку подключения к ресурсам
}
