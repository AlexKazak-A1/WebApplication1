using Microsoft.AspNetCore.Mvc;
using static WebApplication1.Models.ConnectionModel;

namespace WebApplication1.Data.Interfaces;

public interface IConnectionService
{
    public Task<JsonResult> CheckRancherUrl([FromBody] UrlRancherCheckModel model);
    public Task<JsonResult> CheckProxmoxUrl([FromBody] UrlProxmoxCheckModel model);
}
