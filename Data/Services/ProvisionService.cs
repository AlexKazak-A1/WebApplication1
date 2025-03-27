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

    public ProvisionService(ILogger<ProvisionService> logger, IRancherService rancherService, IProxmoxService proxmoxService, IDBService dBService, IConfiguration configuration)
    {
        _logger = logger;
        _dbWorker = dBService;
        _proxmoxService = proxmoxService;
        _rancherService = rancherService;
        _configuration = configuration;
    }

    public async Task<JsonResult> GetConnectionCreds(ConnectionType connectionTarget)
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

    public async Task<JsonResult> CreateClaster(CreateClusterDTO param)
    {
        if (param == null)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Wrong type in CreateClaster" });
        }

        try
        {

            var creationResult = await _rancherService.CreateClusterAsync(param);

            if (creationResult != null)
            {
                return creationResult;
            }            

            return new JsonResult(new Response { Status = Status.ERROR, Message = $"Check {nameof(CreateClaster)} in {nameof(ProvisionService)}" });
        }
        catch (Exception ex)
        {
            throw new Exception($"Check {nameof(CreateClaster)} in {nameof(ProvisionService)}");
        }
    }

    public async Task<JsonResult> CreateProxmoxVMs(CreateVMsDTO param)
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

    public async Task<JsonResult> GetConnectionStringToRancher(CreateClusterDTO clusterInfo)
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

    public async Task<JsonResult> StartVMAndConnectToRancher(ConnectVmToRancherDTO data)
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

            List<Response> result = new();
            foreach (var vmStatus in vmsReadyStatus)
            {
               if (vmStatus.Value)
               {
                    result.Add(new Response { Status = Status.OK, Message = vmStatus.Key.ToString() });
               }
               else
               {
                    result.Add(new Response { Status = Status.WARNING, Message = vmStatus.Key.ToString() });
               }
            }

            return new JsonResult(result);
            //return new JsonResult(new Response { Status = Status.OK, Message = "test ready status check is Ok" });
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

    public async Task<JsonResult> GetCreationAvailibility(CreateVMsDTO info)
    {
        
        if (info == null)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = $"info is null in {nameof(GetCreationAvailibility)}" });
        }

        try
        {
            var vmAllocation = new Dictionary<string, string>();
            var result = await _proxmoxService.CheckCreationAbility(info);

            if (result != null)
            {
                return new JsonResult(new Response { Status = Status.OK, Message = "Cluster can be Created", Data = result });
            }

            return new JsonResult(new Response { Status = Status.ERROR, Message = "Cluster can`t be Created" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = $"Error in in {nameof(GetCreationAvailibility)}" });
        }
    }
}
