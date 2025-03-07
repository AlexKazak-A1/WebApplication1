using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using Microsoft.AspNetCore.Http.Metadata;

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

    /// <summary>
    /// Returns Json object with list of templates of selected Proxmox cluster.
    /// </summary>
    /// <param name="data">Model with id of proxmox from DB</param>
    /// <returns>Returns Json object with SelectOptionDTO(Value = string, Text = string).</returns>
    /// <response code="200">If DB is accecible. Selects all available data</response>
    /// <response code="500">If an exception is thrown.</response>
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

    /// <summary>
    /// Returns Json object with list of template params of selected template.
    /// </summary>
    /// <param name="data">Model with id of template of Proxmox host</param>
    /// <returns>Returns Json object with TemplateParams(CPU = string, RAM = string, HDD = string).</returns>
    /// <response code="200">If template is available.</response>
    /// <response code="500">If an exception is thrown.</response>
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

    /// <summary>
    /// Creates credentials for Proxmox cluster/host access
    /// </summary>
    /// <param name="model">Represents ProxmoxModel</param>
    /// <returns>Returns JSON object with Responce(Status = int, Message = string).</returns>
    /// <response code="200">If creds was added correctly.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
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

    /// <summary>
    /// Gets all currently available Proxmox hosts in Proxomox Cluster
    /// </summary>
    /// <param name="data">Model with id of proxmox from DB.</param>
    /// <returns>List of Proxmox host names.</returns>
    /// <response code="200">If Proxmox available.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> GetProxmoxHosts([FromBody] ProxmoxIdDTO data)
    {
        try
        {
            var result = (await _proxmoxService.GetProxmoxNodesListAsync(data.ProxmoxId)).Where( x => x.Status == "online").Select(x => x.Node).ToList().OrderBy(x => x).ToList();
            return Ok( new JsonResult(result));
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = $"Check {nameof(GetProxmoxHosts)} in {nameof(ProxmoxController)}" }));
        }
    }

    /// <summary>
    /// Gets JSON object with List of storages name.
    /// </summary>
    /// <param name="data">Model with id of proxmox from DB</param>
    /// <returns>List of storages name for current Proxmox host</returns>
    /// <response code="200">If Proxmox available.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> GetProxmoxStorages([FromBody] ProxmoxIdDTO data)
    {
        try
        {
            var result = (await _proxmoxService.GetProxmoxStoragesAsync(data.ProxmoxId)).Select(x => x.Storage).Distinct().OrderBy(x => x).ToList();
            return Ok(new JsonResult(result));
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = $"Check {nameof(GetProxmoxHosts)} in {nameof(ProxmoxController)}" }));
        }
    }
}
