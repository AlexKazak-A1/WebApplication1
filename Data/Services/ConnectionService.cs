using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Data.Services;

public class ConnectionService : IConnectionService
{
    public async Task<JsonResult> CheckRancherUrl([FromBody] UrlRancherCheckModel model)
    {
        if (string.IsNullOrEmpty(model.Url))
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "URL is required." });
        }

        if (string.IsNullOrEmpty(model.Token))
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "Token is required." });
        }
        model.Url += "/v3";
        bool isValid = await CheckUrlAvailabilityAsync(model.Url, isBearer: true, model.Token);
        return new JsonResult(new UrlCheckResponse
        {
            IsValid = isValid,
            Message = isValid ? "URL is accessible." : "URL is not accessible."
        });
    }

    public async Task<JsonResult> CheckProxmoxUrl([FromBody] UrlProxmoxCheckModel model)
    {
        if (string.IsNullOrEmpty(model.Url))
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "URL is required." });
        }

        if (string.IsNullOrEmpty(model.UserName))
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "UserName is required." });
        }

        if (string.IsNullOrEmpty(model.TokenID) || string.IsNullOrEmpty(model.TokenSecret))
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "Token is required. Check bouth TokenID & TokenSecret" });
        }

        model.Url += "/api2/json/version";

        bool isValid = await CheckUrlAvailabilityAsync(url: model.Url, isBearer: false, token: model.TokenID, user: model.UserName, tokenSectet: model.TokenSecret);
        return new JsonResult(new UrlCheckResponse
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
}
