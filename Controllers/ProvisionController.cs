﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Text;
using System.Text.Json.Nodes;
using WebApplication1.Data;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class ProvisionController : Controller
{
    private readonly ILogger<ProvisionController> _logger;
    private string _rancherCreatePayload = string.Empty;
    private IProvision _dbWorker;

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

    public ProvisionController(ILogger<ProvisionController> logger, IProvision provision)
    {
        _logger = logger;
        _rancherCreatePayload = SetPayload();
        _dbWorker = provision;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetConnectionCreds([FromBody] object connectionTarget)
    {
        try
        {
            // Attempt to parse connectionTarget into the ConnectionType enum
            var type = Enum.Parse<ConnectionType>(connectionTarget.ToString(), true);

            // Process the valid connection type
            var result = await GetCreds(type);
            return Ok( Json(result));
        }
        catch (ArgumentException)
        {
            // Return error if connectionTarget is not a valid ConnectionType
            return BadRequest( Json(new Response { Status = Status.ERROR, Message = "No such connection type" }));
        }
    }


    [HttpPost]
    public async Task<IActionResult> CreateClaster([FromBody] object data)
    {
        try
        {
            var param = JsonConvert.DeserializeObject<CreateClusterDTO>(data.ToString());

            if (param != null)
            {
                var payload = SetPayload((param)?.ClasterName);

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
                if (response.IsSuccessStatusCode)
                {
                    return Ok( Json( new Response { Status = Status.OK, Message = "Cluster created successfully" }));
                }
                else
                {
                    var resultTest = JsonConvert.DeserializeObject<RancherResponse>( await response.Content.ReadAsStringAsync());
                    return BadRequest( Json( new Response { Status = (Status)int.Parse(resultTest.Code), Message = resultTest.Message }));
                }           
            }

            return BadRequest( Json( new Response { Status = Status.ERROR, Message = "Wrong type in CreateClaster" }));
        }
        catch (Exception ex)
        {
            return BadRequest( Json( new Response { Status = Status.ERROR, Message = $"Check CreateClaster() method\n{ex.Message}" }));
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateProxmoxVMs([FromBody] object data)
    {
        try
        {
            var param = JsonConvert.DeserializeObject<CreateVMsDTO>(data.ToString());

            Proxmox proxmox = new Proxmox(_logger, _dbWorker)
            {
                EtcdAndControlPlaneAmount = param.EtcdAndCPlaneAmount,
                ProxmoxID = param.ProxmoxId,
                WorkersAmount = param.WorkerAmount,
                Rancher = new CreateClusterDTO { ClasterName = param.ClusterName, RancherId = param.RancherId.ToString() },
                VMTemplateID = param.VMTemplateId,
                VMStartIndex = param.VMStartIndex,
                VMPrefix = param.VMPrefix,
            };

            var creationVMsResult = await proxmox.StartProvisioningVMsAsync();

            var responseList = new List<Response>();

            foreach (var str in creationVMsResult as List<string>)
            {
                if (str.Contains("Successfully"))
                {
                    responseList.Add(new Response { Status = Status.OK, Message = str });
                }
                else
                {
                    responseList.Add(new Response { Status = Status.WARNING, Message = str });
                }
                
            }

            //var message = JsonConvert.SerializeObject(responseList);

            return Ok( Json(responseList));
        }
        catch (Exception ex)
        {
            return BadRequest( Json( new Response { Status = Status.ERROR, Message = ex.Message }));
        }
    }

    private async Task<List<SelectOptionDTO>> GetCreds(object inputType)
    {
        var selectList = new List<SelectOptionDTO>();

        if (inputType is ConnectionType type)
        {
            if (type == ConnectionType.Proxmox)
            {
                var currentTypeArray = new List<ProxmoxModel>((List<ProxmoxModel>) await _dbWorker.GetConnectionCredsAsync(type));

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
                var currentTypeArray = new List<RancherModel>((List<RancherModel>) await _dbWorker.GetConnectionCredsAsync(type));

                foreach(var rancher in currentTypeArray)
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

    public string SetPayload(string newClusterName = "NewClusterName")
    {
        // Updating name of new cluster
        var stringJson = "{\"type\":\"provisioning.cattle.io.cluster\",\"metadata\":{\"namespace\":\"fleet-default\",\"name\":\"testing\"},\"spec\":{\"rkeConfig\":{\"chartValues\":{\"rke2-calico\":{}},\"upgradeStrategy\":{\"controlPlaneConcurrency\":\"1\",\"controlPlaneDrainOptions\":{\"deleteEmptyDirData\":true,\"disableEviction\":false,\"enabled\":false,\"force\":false,\"gracePeriod\":-1,\"ignoreDaemonSets\":true,\"skipWaitForDeleteTimeoutSeconds\":0,\"timeout\":120},\"workerConcurrency\":\"1\",\"workerDrainOptions\":{\"deleteEmptyDirData\":true,\"disableEviction\":false,\"enabled\":false,\"force\":false,\"gracePeriod\":-1,\"ignoreDaemonSets\":true,\"skipWaitForDeleteTimeoutSeconds\":0,\"timeout\":120}},\"machineGlobalConfig\":{\"cni\":\"calico\",\"disable-kube-proxy\":false,\"etcd-expose-metrics\":false},\"machineSelectorConfig\":[{\"config\":{\"protect-kernel-defaults\":false}}],\"etcd\":{\"disableSnapshots\":false,\"s3\":null,\"snapshotRetention\":5,\"snapshotScheduleCron\":\"0 */5 * * *\"},\"registries\":{\"configs\":{},\"mirrors\":{}},\"machinePools\":[]},\"machineSelectorConfig\":[{\"config\":{}}],\"kubernetesVersion\":\"v1.26.15+rke2r1\",\"defaultPodSecurityPolicyTemplateName\":\"\",\"defaultPodSecurityAdmissionConfigurationTemplateName\":\"\",\"localClusterAuthEndpoint\":{\"enabled\":false,\"caCerts\":\"\",\"fqdn\":\"\"}}}";
        dynamic jsonObject = JsonConvert.DeserializeObject<dynamic>(stringJson);
        jsonObject.metadata.name = newClusterName;
        stringJson = JsonConvert.SerializeObject(jsonObject);

        return stringJson;
    }
}
