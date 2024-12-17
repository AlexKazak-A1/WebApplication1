using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using System.Net.Http;
using System.Threading.Tasks;
using static WebApplication1.Models.ConnectionModel;
using System.Runtime.CompilerServices;
using WebApplication1.Data;
using WebApplication1.Data.WEB;

namespace WebApplication1.Controllers;

public class RancherController : Controller
{
    private readonly ILogger<RancherController> _logger;
    private readonly IDBService _dbWorker;

    public RancherController(ILogger<RancherController> logger,IDBService worker)
    {
        _logger = logger;
        _dbWorker = worker;
    }
    public IActionResult Index()
    {
        return View(new CombinedModel());
    }

    public IActionResult AddNewRancher()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateNewRancherCred([FromBody] RancherModel model)
    {
        if (model == null)
        {
            return Ok(Json(new Response { Status = Status.ERROR, Message = "No data Provided" }));
        }

        try
        {
            var rancherService = new RancherService(_dbWorker);

            return Ok( await rancherService.CreateNewRancherCred(model));
        }
        catch (Exception ex)
        {
            return BadRequest(Json(new Response { Status = Status.ERROR, Message = $"Some error in {nameof(CreateNewRancherCred)}" }));
        }
    }

    //[HttpPost]
    //public async Task<JsonResult> CreateNewRancherCred([FromBody] RancherModel model)
    //{
    //    if (string.IsNullOrEmpty(model.RancherURL))
    //    {
    //        return Json(new { Status = Status.WARNING, Message = "Rancher url is empty" });
    //    }

    //    if (string.IsNullOrEmpty(model.RancherToken))
    //    {
    //        return Json(new { Status = Status.WARNING, Message = "Rancher Token is empty" });
    //    }

    //    if (!await _dbWorker.CheckDBConnection())
    //    {
    //        return Json(new { Status = Status.ERROR, Message = "Database is unreachable" });
    //    }

    //    if (await _dbWorker.AddNewCred(model))
    //    {
    //        return Json(new { Status = Status.OK, Message = "New Rancher Creds was successfully added" });
    //    }

    //    return Json(new { Status = Status.WARNING, Message = "New Rancher Creds wasn`t added.\n" +
    //        "Maybe you try to add existing data.\n Contact an adnimistrator"
    //    });
    //}

}
