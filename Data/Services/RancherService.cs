using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using Newtonsoft.Json;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Enums;

namespace WebApplication1.Data.Services;

public class RancherService : IRancherService
{
    private readonly IDBService _dbWorker;
    private readonly ILogger<RancherService> _logger;

    public RancherService (ILogger<RancherService> logger, IDBService dbWorker)
    {
        _logger = logger;
        _dbWorker = dbWorker;
    }

    public async Task<JsonResult> CreateNewRancherCred([FromBody] RancherModel model)
    {
        if (string.IsNullOrEmpty(model.RancherURL))
        {
            return new JsonResult(new { Status = Status.WARNING, Message = "Rancher url is empty" });
        }

        if (string.IsNullOrEmpty(model.RancherToken))
        {
            return new JsonResult(new { Status = Status.WARNING, Message = "Rancher Token is empty" });
        }

        if (!await _dbWorker.CheckDBConnection())
        {
            return new JsonResult(new { Status = Status.ERROR, Message = "Database is unreachable" });
        }

        if (await _dbWorker.AddNewCred(model))
        {
            return new JsonResult(new { Status = Status.OK, Message = "New Rancher Creds was successfully added" });
        }

        return new JsonResult(new
        {
            Status = Status.WARNING,
            Message = "New Rancher Creds wasn`t added.\n" +
            "Maybe you try to add existing data.\n Contact an adnimistrator"
        });
    }

    public async Task<string> GetConnectionString(string RancherId, string ClusterName)
    {
        if (RancherId == null || ClusterName == null)
        {
            return string.Empty;
        }

        try
        {
            var currentRancher = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Rancher) as List<RancherModel>).First(x => x.Id == int.Parse(RancherId));

            await Task.Delay(1500);
            var clusterId = await GetCurrentClusterID(currentRancher.RancherURL, currentRancher.RancherToken, ClusterName);

            var insecureConnString = await GetInsecureConnectionString(currentRancher.RancherURL, currentRancher.RancherToken, clusterId);


            return insecureConnString;
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
    }

    private async Task<string> GetCurrentClusterID(string url, string token, string clusterName)
    {
        url += "/v1/provisioning.cattle.io.clusters?exclude=metadata.managedFields";
        var response = await PostToRancher(url, token);
        var result = JsonConvert.DeserializeObject<List<ClusterStatusDTO>>(response);

        foreach (var item in result)
        {
            if (item.Metada.Name.Equals(clusterName))
            {
                return item.Status.ClusterName;
            }
        }

        return "";
    }

    private async Task<string> GetInsecureConnectionString(string url, string token, string clusterID)
    {
        url += "/v3/clusterregistrationtokens";
        var responce = await PostToRancher(url, token);
        var data = JsonConvert.DeserializeObject<List<RancherClucterRegistrationDTO>>(responce);

        return data.Where(x => x.ClusterId.Equals(clusterID)).FirstOrDefault().InsecureNodeCommand;
    }

    private async Task<string> PostToRancher(string url, string token)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var httpClient = new HttpClient(handler);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


        var response = await httpClient.SendAsync(request);
        var resultTest = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RancherResponse>(resultTest);
        return result.Data.ToString();
    }
}
