using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace WebApplication1.Controllers;

[Authorize]
public class RancherController : Controller
{
    private readonly ILogger<RancherController> _logger;

    public RancherController(ILogger<RancherController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new CombinedModel());
    }

    public async Task<IActionResult> AddNewRancher()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        ViewBag.AccessToken = accessToken;
        return View();
    }    
}
