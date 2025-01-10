using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using static WebApplication1.Models.ConnectionModel;

namespace WebApplication1.Controllers;

public class ConnectionController : Controller
{
    private readonly ILogger<ConnectionController> _logger;

    public ConnectionController(ILogger<ConnectionController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new CombinedModel());
    }

   

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
