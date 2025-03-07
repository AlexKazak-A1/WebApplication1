using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Models;

public class CombinedModel: PageModel
{
    public UrlRancherCheckModel RancherCheckModel { get; set; }
    public UrlProxmoxCheckModel ProxmoxCheckModel { get; set; }
    public UrlCheckResponse UrlCheckResponse { get; set; }
}
