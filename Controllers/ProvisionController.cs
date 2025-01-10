using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Text;
using System.Text.Json.Nodes;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.Services;
using WebApplication1.Data.WEB;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class ProvisionController : Controller
{
    private readonly ILogger<ProvisionController> _logger;   

    public ProvisionController(ILogger<ProvisionController> logger)
    {
        _logger = logger;        
    }

    public IActionResult Index()
    {
        return View();
    }

}
