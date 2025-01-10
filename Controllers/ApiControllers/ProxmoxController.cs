using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Models.ConnectionModel;
using System.Diagnostics;
using WebApplication1.Models;
using Newtonsoft.Json;
using WebApplication1.Data;
using WebApplication1.Data.Interfaces;
using System.Globalization;
using WebApplication1.Data.Database;
using WebApplication1.Data.Enums;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.Services;
using WebApplication1.Data.WEB;

namespace WebApplication1.Controllers.ApiControllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ProxmoxController : ControllerBase
{
    private readonly ILogger<ProxmoxController> _logger;
    private readonly IProxmoxService _proxmoxService;

    public ProxmoxController(ILogger<ProxmoxController> logger, IProxmoxService worker)
    {
        _logger = logger;
        _proxmoxService = worker;
    }

    [HttpPost]
    public async Task<IActionResult> GetTemplate([FromBody] ProxmoxIdDTO data)
    {
        try
        {           
            var result = await _proxmoxService.GetTemplate(data);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new List<SelectOptionDTO>()));
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetTemplateParams([FromBody] TemplateIdDTO data)
    {
        try
        {
            var result = await _proxmoxService.GetTemplateParams(data);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new TemplateParams { }));
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateNewProxmoxCred([FromBody] ProxmoxModel model)
    {
        try
        {
            var result = await _proxmoxService.CreateNewProxmoxCred(model);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = $"Check {nameof(CreateNewProxmoxCred)} in {nameof(ProxmoxController)}" }));
        }
    }
}
