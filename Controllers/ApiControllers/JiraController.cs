using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Data.Jira;
using WebApplication1.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebApplication1.Controllers.ApiControllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/[controller]/[action]")]
[ApiController]
public class JiraController : ControllerBase
{
    private readonly ILogger<JiraController> _logger;
    private readonly IJiraService _jiraService;
    private readonly IServiceProvider _provider;

    public JiraController(ILogger<JiraController> logger, IJiraService jiraService, IServiceProvider provider)
    {
        _logger = logger;
        _jiraService = jiraService;
        _provider = provider;
    }

    /// <summary>
    /// Initialize proccess of creating New RKE2 cluster on Proxmox
    /// </summary>
    /// <param name="data">Info for creating cluster</param>    
    /// <returns>Returns UID od started proccess</returns>
    /// <response code="200">Returns UID of creation Task</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> CreateClusterLazy([FromBody] JiraCreateClusterRequestDTO data)
    {
        try
        {
            // создаём скоуп вручную, чтобы взять зависимости
            var scope = _provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IJiraService>(); // класс, где CreateClusterLazy

            var taskId = TaskManager.StartClusterCreation(async() => await service.CreateClusterLazy(data));

            return Ok(new { taskId });

            //if (result != null && result is JsonResult res && res.StatusCode == 200)
            //{
            //    return Ok(new JsonResult(new Response { Status = Status.OK, Message = "Cluster creation started." }));
            //}
            //else
            //{
            //    return Ok(new JsonResult(new Response { Status = Status.ERROR, Message = "Cluster wasn't created." }));
            //}
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(CreateClusterLazy)} in {nameof(JiraController)}\n{ex.Message}");
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }

    /// <summary>
    /// Returns info about task.
    /// </summary>
    /// <param name="id">UID of Task for RKE2 cluster creation</param>
    /// <returns>Returns info abouf task</returns>
    /// <response code="200">Returns Task Status</response>
    /// <response code="400">If an exception is thrown or some validation errors.</response>
    [HttpGet]
    public async Task<IActionResult> GetCreationTaskStatus(Guid id)
    {
        var status = TaskManager.GetStatus(id);
        if (status == null)
            return NotFound();

        return Ok(status);
    }

    /// <summary>
    /// Gets all info about Proxmox cluster/host
    /// </summary>
    /// <param name="uniqueProxmoxName">Unique name of Proxmox</param>
    /// <returns>Returns all info about Proxmox cluster/host</returns>
    /// <response code="200">Returns all info about Proxmox cluster/host</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpGet]
    public async Task<IActionResult> InfoCluster(string uniqueProxmoxName)
    {
        try
        {
            return Ok((await _jiraService.GetProxmoxInfo(uniqueProxmoxName)).Value);
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(InfoCluster)} in {nameof(JiraController)}\n{ex.Message}");
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }

    /// <summary>
    /// Gets Info about Proxmox VM in Proxmox cluster/host
    /// </summary>
    /// <param name="uniqueProxmoxName">Unique name of Proxmox</param>
    /// <param name="vmId">Id of VM in Proxmox</param>
    /// <returns>Returls info about VM in Proxmox</returns>
    /// <response code="200">Returns all info about Proxmox cluster/host</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpGet]
    public async Task<IActionResult> GetVMInfo(string uniqueProxmoxName, int vmId)
    {
        try
        {
            return Ok(await _jiraService.GetVMInfo(uniqueProxmoxName, vmId));
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(GetVMInfo)} in {nameof(JiraController)}\n{ex.Message}");
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }
}
