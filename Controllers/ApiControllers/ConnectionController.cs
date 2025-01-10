using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Interfaces;
using static WebApplication1.Models.ConnectionModel;

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
