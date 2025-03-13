using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using WebApplication1.Data.WEB;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Services;
using Microsoft.AspNetCore.Authorization;

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

    public IActionResult AddNewRancher()
    {
        return View();
    }    
}
