using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Data.Interfaces;

public interface IConnectionService
{
    public Task<JsonResult> CheckRancherUrl([FromBody] UrlRancherCheckModel model);
    public Task<JsonResult> CheckProxmoxUrl([FromBody] UrlProxmoxCheckModel model);
}
