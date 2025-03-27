using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

[Authorize]
public class ProvisionController : Controller
{
    private readonly ILogger<ProvisionController> _logger;   

    public ProvisionController(ILogger<ProvisionController> logger)
    {
        _logger = logger;        
    }

    public async Task<IActionResult> Index()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        ViewBag.AccessToken = accessToken;
        return View();
    }

}
