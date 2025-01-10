using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Database;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.Services;
using WebApplication1.Data.WEB;
using WebApplication1.Models;

namespace WebApplication1.Controllers.ApiControllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class RancherController : ControllerBase
{
    private readonly ILogger<RancherController> _logger;
    private readonly IRancherService _rancherService;

    public RancherController(ILogger<RancherController> logger, IRancherService rancherService)
    {
        _logger = logger;
        _rancherService = rancherService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateNewRancherCred([FromBody] RancherModel model)
    {
        if (model == null)
        {
            _logger.LogError($"{nameof(CreateNewRancherCred)} \nNo data Provided");
            return Ok(new JsonResult(new Response { Status = Status.ERROR, Message = "No data Provided" }));
        }

        try
        {            
            return Ok(await _rancherService.CreateNewRancherCred(model));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Some error in {nameof(CreateNewRancherCred)}");
            return BadRequest(new JsonResult(new Response { Status = Status.ERROR, Message = $"Some error in {nameof(CreateNewRancherCred)}" }));
        }
    }        
}
