using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace WebApplication1.Controllers;

[Authorize]
public class ProxmoxController : Controller
{
    private readonly ILogger<ProxmoxController> _logger;

    public ProxmoxController(ILogger<ProxmoxController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new ProxmoxModel());
    }

    public async Task<IActionResult> AddNewProxmox()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        ViewBag.AccessToken = accessToken;
        return View(); 
    }    

    public async Task<IActionResult> EditProxmox()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        ViewBag.AccessToken = accessToken;
        return View();
    }
}
