using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using Newtonsoft.Json;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Enums;
using System.Text;
using WebApplication1.Data.WEB;

namespace WebApplication1.Data.Services;

public class RancherService : IRancherService
{
    private readonly IDBService _dbWorker;
    private readonly ILogger<RancherService> _logger;
    private readonly IConfiguration _configuration;

    public RancherService (ILogger<RancherService> logger, IDBService dbWorker, IConfiguration configuration)
    {
        _logger = logger;
        _dbWorker = dbWorker;
        _configuration = configuration;
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

    public async Task<JsonResult> CreateClusterAsync(CreateClusterDTO clusterInfo)
    {
        var payload = SetPayload((clusterInfo)?.ClusterName);
        //Ignore certificate checking
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        using var httpClient = new HttpClient(handler);

        var selectedRancher = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Rancher) as List<RancherModel>).Where(x => x.Id == int.Parse(clusterInfo.RancherId)).First();
        var url = selectedRancher.RancherURL + "/v1/provisioning.cattle.io.clusters";

        // Create the content with the JSON payload
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content,
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", selectedRancher.RancherToken);

        var response = await httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            return new JsonResult(new Response { Status = Status.OK, Message = "Cluster created successfully" });
        }
        else
        {
            var resultTest = JsonConvert.DeserializeObject<RancherResponse>(await response.Content.ReadAsStringAsync());
            if (resultTest!.Code.Equals("AlreadyExists"))
            {
                return new JsonResult(new Response { Status = Status.ALREADY_EXIST, Message = resultTest.Message });
            }

            return new JsonResult(new Response { Status = Status.ERROR, Message = resultTest.Message });
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

    private string SetPayload(string newClusterName = "NewClusterName")
    {
        // Updating name of new cluster
        var stringJson = "{\"type\":\"provisioning.cattle.io.cluster\",\"metadata\":{\"namespace\":\"fleet-default\",\"name\":\"NameHere\"},\"spec\":{\"rkeConfig\":{\"chartValues\":{\"rke2-calico\":{}},\"upgradeStrategy\":{\"controlPlaneConcurrency\":\"1\",\"controlPlaneDrainOptions\":{\"deleteEmptyDirData\":true,\"disableEviction\":false,\"enabled\":false,\"force\":false,\"gracePeriod\":-1,\"ignoreDaemonSets\":true,\"skipWaitForDeleteTimeoutSeconds\":0,\"timeout\":120},\"workerConcurrency\":\"1\",\"workerDrainOptions\":{\"deleteEmptyDirData\":true,\"disableEviction\":false,\"enabled\":false,\"force\":false,\"gracePeriod\":-1,\"ignoreDaemonSets\":true,\"skipWaitForDeleteTimeoutSeconds\":0,\"timeout\":120}},\"machineGlobalConfig\":{\"cni\":\"calico\",\"disable-kube-proxy\":false,\"etcd-expose-metrics\":false},\"machineSelectorConfig\":[{\"config\":{\"protect-kernel-defaults\":false}}],\"etcd\":{\"disableSnapshots\":false,\"s3\":null,\"snapshotRetention\":5,\"snapshotScheduleCron\":\"0 */5 * * *\"},\"registries\":{\"configs\":{},\"mirrors\":{}},\"machinePools\":[]},\"machineSelectorConfig\":[{\"config\":{}}],\"kubernetesVersion\":\"v1.26.15+rke2r1\",\"defaultPodSecurityPolicyTemplateName\":\"\",\"defaultPodSecurityAdmissionConfigurationTemplateName\":\"\",\"localClusterAuthEndpoint\":{\"enabled\":false,\"caCerts\":\"\",\"fqdn\":\"\"},\"agentEnvVars\":[{\"name\":\"HTTP_PROXY\",\"value\":\"HTTP_PROXY_VALUE\"},{\"name\":\"HTTPS_PROXY\",\"value\":\"HTTPS_PROXY_VALUE\"},{\"name\":\"NO_PROXY\",\"value\":\"NO_PROXY_VALUE\"}]}}";
        stringJson = stringJson.Replace("NameHere", newClusterName);
        stringJson = stringJson.Replace("HTTP_PROXY_VALUE", _configuration["HTTP_PROXY"] ?? "http://10.254.49.150:3128");
        stringJson = stringJson.Replace("HTTPS_PROXY_VALUE", _configuration["HTTPS_PROXY"] ?? "http://10.254.49.150:3128");
        stringJson = stringJson.Replace("NO_PROXY_VALUE", _configuration["NO_PROXY"] ?? "127.0.0.0/8,10.0.0.0/8,172.16.0.0/12,192.168.0.0/16,.svc,.cluster.local,rancher.a1by.tech,.main.velcom.by");

        return stringJson;
    }
}
