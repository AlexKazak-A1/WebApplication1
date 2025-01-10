using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using System.Net.Http;
using System.Threading.Tasks;
using static WebApplication1.Models.ConnectionModel;
using System.Runtime.CompilerServices;
using WebApplication1.Data.WEB;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Services;

namespace WebApplication1.Controllers;

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
