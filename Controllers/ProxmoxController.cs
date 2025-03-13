using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;
using WebApplication1.Data.Services;
using Microsoft.AspNetCore.Authorization;

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

    public IActionResult AddNewProxmox()
    {
        return View(); 
    }    
}
