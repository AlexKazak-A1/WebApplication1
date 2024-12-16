using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Models;

public class ConnectionModel : PageModel
{
    public class UrlRancherCheckModel
    {
        public string Url { get; set; }
        public string Token { get; set; }
    }

    public class UrlProxmoxCheckModel
    {
        public string Url { get; set; }
        public string UserName { get; set; }
        public string TokenID { get; set; }
        public string TokenSecret { get; set; }        
    }

    public class UrlCheckResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }

    public class CombinedModel
    {
        public UrlRancherCheckModel RancherCheckModel { get; set; }
        public UrlProxmoxCheckModel ProxmoxCheckModel { get; set; }
        public UrlCheckResponse UrlCheckResponse { get; set; }
    }
}
