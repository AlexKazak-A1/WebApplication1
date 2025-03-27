using Newtonsoft.Json;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Jira;
using WebApplication1.Data.WEB;
using JsonResult = Microsoft.AspNetCore.Mvc.JsonResult;
using WebApplication1.Data.ProxmoxDTO;

namespace WebApplication1.Data.Services;

public class JiraService : IJiraService
{
    private readonly ILogger<JiraService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IRancherService _rancherService;
    private readonly IProxmoxService _proxmoxService;
    private readonly IProvisionService _provisionService;

    public JiraService(ILogger<JiraService> logger, IConfiguration configuration, IProxmoxService proxmoxService, IRancherService rancherService, IProvisionService provisionService)
    {
        _logger = logger;
        _configuration = configuration;
        _proxmoxService = proxmoxService;
        _rancherService = rancherService;
        _provisionService = provisionService;
    }

    public async Task<object?> CreateClusterLazy(JiraCreateClusterRequestDTO data)
    {
        try
        {
            var creationAbility = await _proxmoxService.CheckCreationAbility(data.CreateVMsDTO);

            if (creationAbility != null)
            {
                data.CreateVMsDTO.ProvisionSchema = creationAbility;
            }

            var createRancherCluster = await _rancherService.CreateClusterAsync(data.CreateClusterDTO);

            if (createRancherCluster.Value is Response response && response.Status == Enums.Status.ERROR)
            {
                return null;
            }

            var connectionToRancherString = await _rancherService.GetConnectionString(data.CreateClusterDTO.RancherId, data.CreateClusterDTO.ClusterName);

            if (string.IsNullOrEmpty(connectionToRancherString))
            {
                return null;
            }

            var createProxomxVMs = await _provisionService.CreateProxmoxVMs(data.CreateVMsDTO);

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
            connection.ProxmoxId = data.CreateVMsDTO.ProxmoxId.ToString();
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

    public async Task<JsonResult> GetProxmoxInfo(int proxmoxId)
    {
        return new JsonResult(await _proxmoxService.GetProxmoxResources(proxmoxId));
    }

    public async Task<VmInfoDTO> GetVMInfo(int proxmoxId, int VMId)
    {
        return await _proxmoxService.GetVmInfoAsync(proxmoxId, VMId);
    }
}
