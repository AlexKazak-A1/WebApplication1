using Newtonsoft.Json;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Jira;
using WebApplication1.Data.WEB;
using JsonResult = Microsoft.AspNetCore.Mvc.JsonResult;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;

namespace WebApplication1.Data.Services;

public class JiraService : IJiraService
{
    private readonly ILogger<JiraService> _logger;    
    private readonly IRancherService _rancherService;
    private readonly IProxmoxService _proxmoxService;
    private readonly IProvisionService _provisionService;

    public JiraService(ILogger<JiraService> logger, IProxmoxService proxmoxService, IRancherService rancherService, IProvisionService provisionService)
    {
        _logger = logger;
        _proxmoxService = proxmoxService;
        _rancherService = rancherService;
        _provisionService = provisionService;
    }

    public async Task<object?> CreateClusterLazy(JiraCreateClusterRequestDTO data)
    {
        try
        {
            var createVMsDTO = new CreateVMsDTO();

            var createClusterDTO = new CreateClusterDTO();
            createClusterDTO.ClusterName = data.ClusterName;
            createVMsDTO.ClusterName = data.ClusterName;

            var rancher = await _rancherService.GetRancherCred(data.UniqueRancherName);
            var proxmox = await _proxmoxService.GetProxmoxCred(data.UniqueProxmoxName);

            createClusterDTO.RancherId = rancher != -1 ? rancher.ToString() : null;
            
            createVMsDTO.RancherId = rancher;
            createVMsDTO.ProxmoxId = proxmox;


            if (createClusterDTO.RancherId == null || createVMsDTO.ProxmoxId == -1 || createVMsDTO.RancherId == -1)
            {
                return null;
            }

            createVMsDTO = await _proxmoxService.SetProxmoxDefaultValues(createVMsDTO, data.Environment);

            if (createVMsDTO == null)
            {
                return null;
            }

            createVMsDTO.WorkerAmount = data.WorkerAmount;
            createVMsDTO.VMConfig = data.ParamsPerWorker;


            var creationAbility = await _proxmoxService.CheckCreationAbility(createVMsDTO);

            if (creationAbility != null)
            {
                createVMsDTO.ProvisionSchema = creationAbility;
            }

            var createRancherCluster = await _rancherService.CreateClusterAsync(createClusterDTO);

            if (createRancherCluster.Value is Response response && response.Status == Enums.Status.ERROR)
            {
                return null;
            }

            var connectionToRancherString = await _rancherService.GetConnectionString(createClusterDTO.RancherId, createClusterDTO.ClusterName);

            if (string.IsNullOrEmpty(connectionToRancherString))
            {
                return null;
            }

            var createProxomxVMs = await _provisionService.CreateProxmoxVMs(createVMsDTO);

            var vms = new List<int>();

            if (!(createProxomxVMs.Value is List<Response> responses))
            {
                return null;
            }

            foreach (var res in responses)
            {
                if(res.Status == Enums.Status.OK || res.Status == Enums.Status.ALREADY_EXIST)
                {
                    vms.Add(int.Parse(res.Message));
                }
            }

            var connection = new ConnectVmToRancherDTO();
            connection.ConnectionString = connectionToRancherString;
            connection.ProxmoxId = createVMsDTO.ProxmoxId.ToString();
            connection.VMsId = vms;

            var result = await _provisionService.StartVMAndConnectToRancher(connection);
            result.StatusCode = StatusCodes.Status200OK;

            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in {nameof(CreateClusterLazy)}\n{ex.Message}");
            return null;
        }
    }

    public async Task<JsonResult> GetProxmoxInfo(string proxmoxUniqueName)
    {
        return new JsonResult(await _proxmoxService.GetProxmoxResources(proxmoxUniqueName));
    }

    public async Task<VmInfoDTO> GetVMInfo(string proxmoxUniqueName, int VMId)
    {
        return await _proxmoxService.GetVmInfoAsync(proxmoxUniqueName, VMId);
    }
}
