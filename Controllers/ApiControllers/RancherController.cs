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

    /// <summary>
    /// Creates new Rancher connection creds in DB
    /// </summary>
    /// <param name="model">Model that represents main creds for Rancher connection</param>
    /// <returns>Returns Json object with Responce(Status = int, Message = string)</returns>
    /// <response code="200">If such Rancher creds was added correctly.</response>
    /// <response code="500">If an exception is thrown or some validation errors.</response>
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
