using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.WEB;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebApplication1.Controllers.ApiControllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class ProvisionController : ControllerBase
{
    private readonly ILogger<ProvisionController> _logger;
    private readonly IProvisionService _provisionService;

    public ProvisionController(ILogger<ProvisionController> logger, IProvisionService provisionService)
    {
        _logger = logger;
        _provisionService = provisionService;
    }

    /// <summary>
    /// Search all connections of specified type in DB
    /// </summary>
    /// <param name="connectionTarget">Concret type on ConnectionType enum. Rancher = 0, Proxmox = 1</param>
    /// <returns>Returns Json that contains List of creds of specified type</returns>
    /// <response code="200">If Creds available.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> GetConnectionCreds([FromBody] ConnectionTypeDTO connectionTarget)
    {
        try
        {            
            var result = await _provisionService.GetConnectionCreds(connectionTarget.ConnectionType);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            // Return error if connectionTarget is not a valid ConnectionType
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = "No such connection type" }));
        }
    }

    /// <summary>
    /// Creates New Rancher Cluster
    /// </summary>
    /// <param name="data">Object of CreateClusterDTO</param>
    /// <returns>Returns JSON object Responce(Status = int, Message = string).</returns>
    /// <response code="200">If Rancher cluster was created correctly.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> CreateClaster([FromBody] CreateClusterDTO data)
    {
        try
        {
            var result = await _provisionService.CreateClaster(data);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }

    /// <summary>
    /// Creates all number of VMs in Proxmox
    /// </summary>
    /// <param name="data">Object of CreateVMsDTO</param>
    /// <returns>Returns JSON object Responce(Status = int, Message = string).</returns>
    /// <response code="200">If VMs was created.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> CreateProxmoxVMs([FromBody] CreateVMsDTO data)
    {
        try
        {
            var result = await _provisionService.CreateProxmoxVMs(data);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }

    /// <summary>
    /// Gwts conncetion string for VMs to be able connect to Rancher cluster
    /// </summary>
    /// <param name="clusterInfo">Object of CreateClusterDTO</param>
    /// <returns>Returns JSON object Responce(Status = int, Message = string).</returns>
    /// <response code="200">If Rancher available.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> GetConnectionStringToRancher([FromBody] CreateClusterDTO clusterInfo)
    {
        try
        {
            var result = await _provisionService.GetConnectionStringToRancher(clusterInfo);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }

    /// <summary>
    /// Trys to start created Proxmox VMs and config them to work as RKE2 cluster.
    /// </summary>
    /// <param name="data">Object of ConnectVmToRancherDTO</param>
    /// <returns>Returns JSON object Responce(Status = int, Message = string).</returns>
    /// <response code="200">If VMs added corretly or with warnings.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> StartVMAndConnectToRancher([FromBody] ConnectVmToRancherDTO data)
    {
        try
        {
            var result = await _provisionService.StartVMAndConnectToRancher(data);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }

    /// <summary>
    /// Checks if it`s awailable to create specified set of VMs in selected Proxmox cluster
    /// </summary>
    /// <param name="info">Object of CreateVMsDTO</param>
    /// <returns>Returns JSON object Responce(Status = int, Message = string).</returns>
    /// <response code="200">If creation of VMs is available.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
    [HttpPost]
    public async Task<IActionResult> GetCreationAvailibility([FromBody] CreateVMsDTO info)
    {
        try
        {
            var result = await _provisionService.GetCreationAvailibility(info);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }
}
