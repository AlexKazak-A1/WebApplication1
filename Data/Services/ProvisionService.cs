using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;

namespace WebApplication1.Data.Services;

public class ProvisionService : IProvisionService
{
    private readonly ILogger<ProvisionService> _logger;
    private readonly IRancherService _rancherService;
    private readonly IProxmoxService _proxmoxService;
    private readonly IDBService _dbWorker;
    private readonly IConfiguration _configuration;

    private string _rancherCreatePayload = string.Empty;
    public string RancherProvisionPayload
    {
        get => _rancherCreatePayload;
        set
        {
            if (!_rancherCreatePayload.Equals(value))
            {
                _rancherCreatePayload = value;
            }
        }
    }

    public ProvisionService(ILogger<ProvisionService> logger, IRancherService rancherService, IProxmoxService proxmoxService, IDBService dBService, IConfiguration configuration)
    {
        _logger = logger;
        _dbWorker = dBService;
        _proxmoxService = proxmoxService;
        _rancherService = rancherService;
        _configuration = configuration;
        _rancherCreatePayload = SetPayload();
    }

    public async Task<JsonResult> GetConnectionCreds([FromBody] ConnectionType connectionTarget)
    {
        try
        {
            // Attempt to parse connectionTarget into the ConnectionType enum
            //var type = Enum.Parse<ConnectionType>(connectionTarget.ToString(), true);

            // Process the valid connection type
            var result = await GetCreds(connectionTarget);
            return new JsonResult(result);
        }
        catch (ArgumentException)
        {
            // Return error if connectionTarget is not a valid ConnectionType
            return new JsonResult(new Response { Status = Status.ERROR, Message = "No such connection type" });
        }
    }

    public async Task<JsonResult> CreateClaster([FromBody] CreateClusterDTO param)
    {
        if (param == null)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Wrong type in CreateClaster" });
        }

        try
        {                
            var payload = SetPayload((param)?.ClusterName);

            //Ignore certificate checking
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var httpClient = new HttpClient(handler);

            var selectedRancher = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Rancher) as List<RancherModel>).Where(x => x.Id == int.Parse(param.RancherId)).First();
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
                if (resultTest.Code.Equals("AlreadyExists"))
                {
                    return new JsonResult(new Response { Status = Status.ALREADY_EXIST, Message = resultTest.Message });
                }
            }

            return new JsonResult(new Response { Status = Status.ERROR, Message = $"Check {nameof(CreateClaster)} in {nameof(ProvisionService)}" });
        }
        catch (Exception ex)
        {
            throw new Exception($"Check {nameof(CreateClaster)} in {nameof(ProvisionService)}");
        }
    }

    public async Task<JsonResult> CreateProxmoxVMs([FromBody] CreateVMsDTO param)
    {
        try
        {
            var responseList = new List<Response>();

            //var param = JsonConvert.DeserializeObject<CreateVMsDTO>(data.ToString());
            //if (!await _proxmoxService.CheckCreationAbility(param))
            //{
            //    responseList.Add(new Response { Status = Status.ERROR, Message = "Can not create vms, because of resource limit" });
            //}
            var creationVMsResult = await _proxmoxService.StartProvisioningVMsAsync(param);

           

            foreach (var str in creationVMsResult as List<object>)
            {
                if (str is int vmId)
                {
                    responseList.Add(new Response { Status = Status.OK, Message = vmId.ToString() });
                }
                else
                {
                    if (str is string response && response.Contains("already exist"))
                    {
                        responseList.Add(new Response { Status = Status.ALREADY_EXIST, Message = response.Split(' ')[0] });
                    }
                    else
                    {
                        responseList.Add(new Response { Status = Status.WARNING, Message = str.ToString() });
                    }
                }
            }

            return new JsonResult(responseList);
        }
        catch (Exception ex)
        {
            throw new Exception($"Check {nameof(CreateProxmoxVMs)} in {nameof(ProvisionService)}", ex);
        }
    }

    public async Task<JsonResult> GetConnectionStringToRancher([FromBody] CreateClusterDTO clusterInfo)
    {
        if (clusterInfo == null)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Cluster Name not provided!" });
        }

        try
        {
            var connectionString = await _rancherService.GetConnectionString(clusterInfo.RancherId, clusterInfo.ClusterName);
            return new JsonResult(new Response { Status = Status.OK, Message = connectionString });
        }
        catch (Exception ex)
        {
           throw new Exception($"Check {nameof(GetConnectionStringToRancher)} in {nameof(ProvisionService)}", ex);
        }
    }

    public async Task<JsonResult> StartVMAndConnectToRancher([FromBody] ConnectVmToRancherDTO data)
    {
        if (data == null)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Input data can't be null" });
        }


        if (!int.TryParse(data.ProxmoxId, out int currentProxmoxId))
        {
            return new JsonResult(new Response { Status = Status.WARNING, Message = "Wrong type of ProxmoxId" });
        }

        try
        {
            var vmsRunningState = await _proxmoxService.StartVmsAsync(data.VMsId, currentProxmoxId);

            var vmsReadyStatus = await _proxmoxService.WaitReadyStatusAsync(vmsRunningState, currentProxmoxId, data.ConnectionString);

            return new JsonResult(new Response { Status = Status.OK, Message = "test ready status check is Ok" });
        }
        catch (Exception ex)
        {
            throw new Exception($"Check {nameof(StartVMAndConnectToRancher)} in {nameof(ProvisionService)}", ex);
        }
    }
    
    private async Task<List<SelectOptionDTO>> GetCreds(object inputType)
    {
        var selectList = new List<SelectOptionDTO>();

        if (inputType is ConnectionType type)
        {
            if (type == ConnectionType.Proxmox)
            {
                var currentTypeArray = new List<ProxmoxModel>((List<ProxmoxModel>)await _dbWorker.GetConnectionCredsAsync(type));

                foreach (var proxmox in currentTypeArray)
                {
                    selectList.Add(new SelectOptionDTO
                    {
                        Value = proxmox.Id.ToString(),
                        Text = proxmox.ProxmoxURL,
                    });
                }

            }
            else if (type == ConnectionType.Rancher)
            {
                var currentTypeArray = new List<RancherModel>((List<RancherModel>)await _dbWorker.GetConnectionCredsAsync(type));

                foreach (var rancher in currentTypeArray)
                {
                    selectList.Add(new SelectOptionDTO
                    {
                        Value = rancher.Id.ToString(),
                        Text = rancher.RancherURL,
                    });
                }

            }
        }

        return selectList;
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

    public async Task<JsonResult> GetCreationAvailibility(CreateVMsDTO info)
    {
        
        if (info == null)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = $"info is null in {nameof(GetCreationAvailibility)}" });
        }

        try
        {
            var result = await _proxmoxService.CheckCreationAbility(info);

            if (result)
            {
                return new JsonResult(new Response { Status = Status.OK, Message = "Cluster can be Created" });
            }

            return new JsonResult(new Response { Status = Status.ERROR, Message = "Cluster can`t be Created" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = $"Error in in {nameof(GetCreationAvailibility)}" });
        }
    }
}
