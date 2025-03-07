using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Controllers.ApiControllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class ConnectionController : ControllerBase
{
    private readonly ILogger<ConnectionController> _logger;
    private readonly IConnectionService _connectionService;

    public ConnectionController(IConnectionService connectionService, ILogger<ConnectionController> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }

    // Метод для проверки доступности Rancher URL

    /// <summary>
    /// Method fo checking Rancher URL availability
    /// </summary>
    /// <param name="model">Represent UrlRancherCheckModel</param>
    /// <returns>Returns the json object representing UrlCheckResponse { IsValid = bool, Message = string }</returns>
    /// <response code="200">If such Rancher URL is accessible.</response>
    /// <response code="500">If an exception is thrown.</response>
    [HttpPost]
    public async Task<IActionResult> CheckRancherUrl([FromBody] UrlRancherCheckModel model)
    {
        try
        {
            return Ok(await _connectionService.CheckRancherUrl(model));
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }

    // Метод для проверки доступности Proxmox URL

    /// <summary>
    /// Method fo checking Proxmox URL availability
    /// </summary>
    /// <param name="model">Represent UrlProxmoxCheckModel</param>
    /// <returns>Returns the json object representing UrlCheckResponse { IsValid = bool, Message = string }</returns>
    /// <response code="200">If such Proxmox URL is accessible.</response>
    /// <response code="500">If an exception is thrown.</response>
    [HttpPost]
    public async Task<IActionResult> CheckProxmoxUrl([FromBody] UrlProxmoxCheckModel model)
    {
        try
        {
            var result = await _connectionService.CheckProxmoxUrl(model);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }
}
