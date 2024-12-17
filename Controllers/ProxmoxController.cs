using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class ProxmoxController : Controller
{
    private readonly ILogger<ProxmoxController> _logger;
    private readonly IDBService _dbWorker;

    public ProxmoxController(ILogger<ProxmoxController> logger, IDBService worker)
    {
        _logger = logger;
        _dbWorker = worker;
    }

    public IActionResult Index()
    {
        return View(new ProxmoxModel());
    }

    public IActionResult AddNewProxmox()
    {
        return View(); 
    }

    [HttpPost]
    public async Task<IActionResult> GetTemplate([FromBody] object? data = null)
    {
        try
        {
            var proxmoxId = int.Parse(data?.ToString());

            var options = new List<SelectOptionDTO>();

            var nodesList = new List<string>();

            var t = (await new ProxmoxService(_logger, _dbWorker).GetProxmoxNodesListAsync(proxmoxId) as List<ProxmoxNodeInfoDTO>);
            nodesList.AddRange(t.Select(x => x.Node));
            //var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();
            if (nodesList.Count > 0)
            {
                var templates = await new ProxmoxService(_logger, _dbWorker).GetAllNodesTemplatesIds(nodesList, proxmoxId);

                foreach (var template in templates as Dictionary<int,string>)
                {
                    options.Add(new SelectOptionDTO { Value = template.Key.ToString(), Text = template.Value });
                }
                return Ok(Json(options));
            }
            else
            {
                return BadRequest(Json(options));
            }
        }
        catch (Exception ex)
        {
            return BadRequest(Json(new List<SelectOptionDTO>()));
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetTemplateParams([FromBody] object data)
    {
        try
        {
            return Ok(Json(new TemplateParams { CPU = 6.ToString(), RAM = 5.6.ToString(CultureInfo.InvariantCulture), HDD = 200.ToString() }));
        }
        catch (Exception ex)
        {
            return BadRequest(Json(new TemplateParams { }));
        }
    }

    [HttpPost]
    public async Task<JsonResult> CreateNewProxmoxCred([FromBody] ProxmoxModel model) 
    {
        
        if (string.IsNullOrEmpty(model.ProxmoxURL))
        {
            return Json(new Response{ Status = Status.WARNING, Message = "Proxmox url is empty" });
        }

        if (string.IsNullOrEmpty(model.ProxmoxToken))
        {
            return Json(new Response { Status = Status.WARNING, Message = "Proxmox Token is empty" });
        }

        if (model.ProxmoxNetTags == null || model.ProxmoxNetTags.Length == 0)
        {                
            return Json(new Response { Status = Status.WARNING, Message = "NO Proxmox VLAN TAGS" });
        }
        else
        {
            for (int i = 0; i < model.ProxmoxNetTags.Length; i++)
            {
                if (model.ProxmoxNetTags[i] == string.Empty)
                {
                    return Json(new Response { Status = Status.WARNING, Message = "One of VLAN TAGS is empty" });
                }
            }
        }

        if (!await _dbWorker.CheckDBConnection())
        {
            return Json(new Response { Status = Status.ERROR, Message = "Database is unreachable" });
        }

        if (await _dbWorker.AddNewCred(model))
        {
            return Json(new Response { Status = Status.OK, Message = "New Proxmox Creds was successfully added" });
        }

        return Json(new Response { Status = Status.WARNING, Message = "New Proxmox Creds wasn`t added.\n" +
            "Maybe you try to add existing data.\n Contact an adnimistrator" });
    }

    

    
}
