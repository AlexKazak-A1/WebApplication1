using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Data.Services;

/// <summary>
/// Служба для работы с соединениями 
/// </summary>
public class ConnectionService : IConnectionService
{
    /// <summary>
    /// ПРОверка наличия подключения к серверу Rancher
    /// </summary>
    /// <param name="model">Описывает необходимые параметры для подключеия к Rancher</param>
    /// <returns>Возвращает Json с результатом проверки</returns>
    public async Task<JsonResult> CheckRancherUrl([FromBody] UrlRancherCheckModel model)
    {
        if (string.IsNullOrEmpty(model.Url)) // проверка пустоты URL
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "URL is required." });
        }

        if (string.IsNullOrEmpty(model.Token)) // проверка пустоты token
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "Token is required." });
        }
        model.Url += "/v3";
        bool isValid = await CheckUrlAvailabilityAsync(model.Url, isBearer: true, model.Token);    // проверка доступности указанного ресурса

        return new JsonResult(new UrlCheckResponse
        {
            IsValid = isValid,
            Message = isValid ? "URL is accessible." : "URL is not accessible."
        });
    }

    /// <summary>
    /// Проверяет доступность кластера/хоста Зкщчьщч
    /// </summary>
    /// <param name="model">Описывает необходимые параметры для проверки подключения</param>
    /// <returns>Возвращает Json с результатом проверки</returns>
    public async Task<JsonResult> CheckProxmoxUrl([FromBody] UrlProxmoxCheckModel model)
    {
        if (string.IsNullOrEmpty(model.Url)) // проверка на пустоту URL
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "URL is required." });
        }

        if (string.IsNullOrEmpty(model.UserName)) // порверка на пустоту имени пользователя
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "UserName is required." });
        }

        if (string.IsNullOrEmpty(model.TokenID) || string.IsNullOrEmpty(model.TokenSecret)) // проверка на пустоту токена и секрера к нему
        {
            return new JsonResult(new UrlCheckResponse { IsValid = false, Message = "Token is required. Check bouth TokenID & TokenSecret" });
        }

        model.Url += "/api2/json/version";

        bool isValid = await CheckUrlAvailabilityAsync(url: model.Url, isBearer: false, token: model.TokenID, user: model.UserName, tokenSectet: model.TokenSecret); // порверка доступности ресурса Proxmox  с указанными параметрами

        return new JsonResult(new UrlCheckResponse
        {
            IsValid = isValid,
            Message = isValid ? "URL is accessible." : "URL is not accessible."
        });
    }


    /// <summary>
    /// Проверка доступности удалёноого хоста
    /// </summary>
    /// <param name="url">Адресс для проверки</param>
    /// <param name="isBearer">Индикатор, используется ли JWT Bearer шифрование</param>
    /// <param name="token">токен для доступа</param>
    /// <param name="user">Optional: имя пользователя(логин), Используется PRoxmox</param>
    /// <param name="tokenSectet">Optional: секрет, используемый Proxmox для подключения пользователя. Используется PRoxmox</param>
    /// <returns></returns>
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

            if (isBearer) // если используется BEarer auth for Rancher
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            else // если используется token for Proxmox
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
