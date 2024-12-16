using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication1.Models;
using static WebApplication1.Models.ConnectionModel;

namespace WebApplication1.Controllers;

public class ConnectionController : Controller
{
    private readonly ILogger<ConnectionController> _logger;

    public ConnectionController(ILogger<ConnectionController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new CombinedModel());
    }

    // Метод для проверки доступности Rancher URL
    [HttpPost]
    public async Task<JsonResult> CheckRancherUrl([FromBody] UrlRancherCheckModel model)
    {
        if (string.IsNullOrEmpty(model.Url))
        {
            return Json(new UrlCheckResponse { IsValid = false, Message = "URL is required." });
        }

        if (string.IsNullOrEmpty(model.Token))
        {
            return Json(new UrlCheckResponse { IsValid = false, Message = "Token is required." });
        }
        model.Url += "/v3";
        bool isValid = await CheckUrlAvailabilityAsync(model.Url, isBearer: true, model.Token);
        return Json(new UrlCheckResponse
        {
            IsValid = isValid,
            Message = isValid ? "URL is accessible." : "URL is not accessible."
        });
    }

    // Метод для проверки доступности Proxmox URL
    [HttpPost]
    public async Task<JsonResult> CheckProxmoxUrl([FromBody] UrlProxmoxCheckModel model)
    {
        if (string.IsNullOrEmpty(model.Url))
        {
            return Json(new UrlCheckResponse { IsValid = false, Message = "URL is required." });
        }

        if (string.IsNullOrEmpty(model.UserName))
        {
            return Json(new UrlCheckResponse { IsValid = false, Message = "UserName is required." });
        }

        if (string.IsNullOrEmpty(model.TokenID) || string.IsNullOrEmpty(model.TokenSecret))
        {
            return Json(new UrlCheckResponse { IsValid = false, Message = "Token is required. Check bouth TokenID & TokenSecret" });
        }

        model.Url += ":8006/api2/json/version";

        bool isValid = await CheckUrlAvailabilityAsync(url: model.Url, isBearer: false, token: model.TokenID, user: model.UserName, tokenSectet: model.TokenSecret);
        return Json(new UrlCheckResponse
        {
            IsValid = isValid,
            Message = isValid ? "URL is accessible." : "URL is not accessible."
        });
    }

    private async Task<bool> CheckUrlAvailabilityAsync(string url, bool isBearer, string token, string user = "", string tokenSectet = "")
    {
        try
        {
            // Настраиваем HttpClientHandler для игнорирования ошибок сертификатов
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var httpClient = new HttpClient(handler);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            if (isBearer)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Authorization", $"PVEAPIToken={user}!{token}={tokenSectet}");
            }

            var response = await httpClient.SendAsync(request);
            var resultTest = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
