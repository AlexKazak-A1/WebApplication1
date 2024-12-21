using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Xml.Linq;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class ProxmoxService
{
    private ILogger _logger;

    public IDBService _dbWorker;

    public ProxmoxService(ILogger logger, IDBService provision = null)
    {
        _logger = logger;
        _dbWorker = provision;
    }

    public async Task<Dictionary<int,bool>> StartVmsAsync(List<int> vmIds, int proxmoxId)
    {
        if (vmIds.Count == 0)
        {
            return new Dictionary<int, bool>();
        }

        var currentProxmox = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();

        if (currentProxmox == null)
        {
            return new Dictionary<int, bool>();
        }

        var node = (await GetProxmoxNodesListAsync(proxmoxId) as List<ProxmoxNodeInfoDTO>).Select(x => x.Node).ToList()[0];

        var resultVMsState = new Dictionary<int, bool>();

        for (int i = 0; i < vmIds.Count; i++) 
        {
            var state = await StartVm(currentProxmox, vmIds[i], node);
            resultVMsState.Add(vmIds[i], state);
        }

        return resultVMsState;
    }

    public async Task<object> StartProvisioningVMsAsync(CreateVMsDTO vmInfo)
    {
        List<object> results = new();
        if (await CheckAllParamsAsync(vmInfo))
        {            
            var isETCDAndControlPlaneCreated = await CreateVmOfType(ClusterElemrntType.ETCDAndControlPlane, vmInfo);
            var isWorkerCreated = await CreateVmOfType(ClusterElemrntType.Worker, vmInfo);

            //await Task.WhenAll(isETCDAndControlPlaneCreated, isWorkerCreated);

            var etcdAndCPlaneStatus = isETCDAndControlPlaneCreated as List<object>;

            var workerStatus = isWorkerCreated as List<object>;

            if (etcdAndCPlaneStatus != null && workerStatus != null)
            {
                foreach (var result in etcdAndCPlaneStatus)
                {

                    results.Add(result);

                    _logger.LogInformation(result.ToString());
                }

                foreach (var result in workerStatus)
                {

                    results.Add(result);

                    _logger.LogInformation(result.ToString());
                }
            }
        }
        return results;
    }    

    public async Task<ProxmoxResponse> SendRequestToProxmoxAsync(string url, HttpMethod httpMethod, string token, object? data = null)
    {
        try
        {
            //Ignore certificate checking
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var httpClient = new HttpClient(handler);
            HttpRequestMessage request = new();
            if (httpMethod == HttpMethod.Post)
            {
                if (data != null)
                {
                    // Create the content with the JSON payload
                    var content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");

                    request = new HttpRequestMessage(httpMethod, url)
                    {
                        Content = content,
                    };
                }
                else
                {
                    request = new HttpRequestMessage(httpMethod, url);
                }
            }
            else if (httpMethod == HttpMethod.Get)
            {
                request = new HttpRequestMessage(httpMethod, url);

            }

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Authorization", token);

            var response = await httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ProxmoxResponse>(result);
        }
        catch (Exception ex)
        {            
            _logger.LogError($"{nameof(ProxmoxService)} \n" + ex.Message);
            return new ProxmoxResponse {  Error = $"Error in {nameof(SendRequestToProxmoxAsync)}", Data = null};
        }
    }

    public async Task<object> GetProxmoxNodesListAsync(int proxmoxId)
    {
        var nodesList = new List<ProxmoxNodeInfoDTO>();

        var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();
        if (proxmoxConn == null)
        {
            return nodesList;
        }

        var response = await new ProxmoxService(_logger).SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes", HttpMethod.Get, proxmoxConn.ProxmoxToken);

        if (response != null && response.Data is JArray jArray)
        {
            var nodesInfoList = jArray.ToObject<List<ProxmoxNodeInfoDTO>>();

            if (nodesInfoList != null)
            {                    
                nodesList.AddRange(nodesInfoList);
            }
        }

        return nodesList;
    }

    public async Task<object> GetAllNodesTemplatesIds(List<string> nodesName, int proxmoxId)
    {
        var templates = new Dictionary<int, string>();

        var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();

        if (nodesName == null && nodesName.Count == 0 && proxmoxConn == null)
        {
            return templates;
        }

        foreach (var node in nodesName)
        {
            var response = await new ProxmoxService(_logger).SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes/{node}/qemu", HttpMethod.Get, proxmoxConn.ProxmoxToken);

            if (response != null && response.Data is JArray jArray)
            {
                var qemuList = jArray.ToObject<List<ProxmoxQemuDTO>>();

                if (qemuList == null)
                {
                    return templates;
                }

                foreach (var qemu in qemuList)
                {
                    if (qemu.Template)
                    {
                        templates.Add(qemu.VmId, qemu.Name);
                    }
                }                
            }
        }
        
        return templates;
    }

    private async Task<bool> CheckAllParamsAsync(CreateVMsDTO vmInfo)
    {
        if (vmInfo?.EtcdAndCPlaneAmount > 0 && vmInfo?.ProxmoxId > 0 && vmInfo?.VMTemplateId > 0 &&
            vmInfo?.WorkerAmount > 0 && vmInfo?.RancherId > 0 && vmInfo.ClusterName != null &&
            this._dbWorker != null && vmInfo?.VMStartIndex > 0)
        {
            if (vmInfo.VMPrefix == string.Empty || vmInfo.VMPrefix == null)
            {
                vmInfo.VMPrefix = "rke2-";
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// Create VM of specified type. Must be used after CheckAllParamsAsync()
    /// </summary>
    /// <param name="elemrntType">Type from ClusterElemrntType enum.</param>
    /// <param name="vmInfo">Instance of CreateVmDTO.</param>
    /// <returns></returns>
    private async Task<object> CreateVmOfType(ClusterElemrntType elemrntType, CreateVMsDTO vmInfo)
    {
        if (_dbWorker == null)
        {
            return false;
        }

        var proxmoxCred = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == vmInfo.ProxmoxId).FirstOrDefault();

        if (proxmoxCred == null)
        {
            return false;
        }

        var nodesList = (await GetProxmoxNodesListAsync(vmInfo.ProxmoxId) as List<ProxmoxNodeInfoDTO>).Select(x => x.Node).ToList();

        if (nodesList == null)
        {
            return false;
        }

        var data = new FullCloneDTO { Full = true, Name = vmInfo.VMPrefix, VMId = vmInfo.VMTemplateId, NewId = vmInfo.VMStartIndex, Node = nodesList[0] };

        var vmList = new List<object>();

        switch (elemrntType)
        {
            case ClusterElemrntType.ETCDAndControlPlane:
                {


                    for (var i = 0; i < vmInfo.EtcdAndCPlaneAmount; i++)
                    {
                        data.Name += "etcd" + i + 1;
                        data.NewId += i;
                        var payload = JsonConvert.SerializeObject(data);

                        vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, nodesList[0], proxmoxCred.ProxmoxToken, vmInfo.VMTemplateId, payload));
                    }

                    return vmList;
                }
            case ClusterElemrntType.Worker:
                {
                    for (var i = 0; i < vmInfo.WorkerAmount; i++)
                    {
                        data.Name += "worker" + i + 1;
                        data.NewId += vmInfo.EtcdAndCPlaneAmount + i;
                        var payload = JsonConvert.SerializeObject(data);

                        vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, nodesList[0], proxmoxCred.ProxmoxToken, vmInfo.VMTemplateId, payload));
                    }

                    return vmList;
                }
            default:
                return false;
        }
    }

    private async Task<object> CreateVM(string proxmoxURL, string nodeName, string accessToken, int vmTemplateId, object payload)
    {
        var newVMID = 0;

        if (payload is string source)
        {
            var elem = "\"newid\":";
            var startIndex = source.IndexOf(elem) + elem.Length;
            var endindex = source.IndexOf(',', startIndex);
            var number = source[startIndex..endindex];
            newVMID = int.Parse(number);
        }

        try
        {
            var nodeVMs = await GetAllVMsId(proxmoxURL, nodeName, accessToken);

            if(nodeVMs.Contains(newVMID))
            {
                return $"{newVMID} already exist";
            }

            var response = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/qemu/{vmTemplateId}/clone", HttpMethod.Post, accessToken, payload);

            var upid = response.Data.ToString();
            

            while (true)
            {
                var status = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/tasks/{upid}/status", HttpMethod.Get, accessToken);
                var result = status.Data.ToString();
                var taskStatus = JsonConvert.DeserializeObject<TaskStatusDTO>(result);

                if (taskStatus != null && taskStatus.Status == "stopped" && taskStatus.ExitStatus != null)
                {
                    if (taskStatus.ExitStatus == "OK")
                    {
                        return newVMID;
                    }
                    else
                    {
                        return $"Error while creating {newVMID} VM";
                    }
                }

                await Task.Delay(100);
            }
        }
        catch (ArgumentNullException ex)
        {
            return "";
        }
        
    }

    private async Task<List<int>> GetAllVMsId(string proxmoxURL, string nodeName, string accessToken)
    {
        var vmList = new List<int>();

        var response = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/qemu/", HttpMethod.Get, accessToken);        

        if (response != null && response.Data is JArray jArray)
        {
            var qemuList = jArray.ToObject<List<ProxmoxQemuDTO>>();

            if (qemuList != null)
            {
                foreach (var qemu in qemuList)
                {
                    vmList.Add(qemu.VmId);
                }
            }
        }
        
        return vmList;
    }

    private async Task<bool> StartVm(ProxmoxModel currentProxmox, int vmId, string nodeName)
    {
        var url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{nodeName}/qemu/{vmId}/status/current";

        var response = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken);
        

        var data = JsonConvert.DeserializeObject<QemuStatusDTO>(response.Data.ToString());
        
        while(true)
        {
            url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{nodeName}/qemu/{vmId}/status/start";          

            response = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken);
            if(response.Data != null)
            {
                url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{nodeName}/qemu/{vmId}/status/current";

                response = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken);
                data = JsonConvert.DeserializeObject<QemuStatusDTO>(response.Data.ToString());

                if (data.Status == "running")
                {
                    break;
                }
            }
            await Task.Delay(3000);
        }

        return true;        
    }

    public async Task<Dictionary<int, bool>> WaitReadyStatusAsync(Dictionary<int, bool> vmsRunningState, int proxmoxId)
    {
        if (vmsRunningState == null || vmsRunningState.Count == 0)
        {
            return new Dictionary<int, bool>();
        }

        var currentProxmox = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();

        if (currentProxmox == null)
        {
            return new Dictionary<int, bool>();
        }

        var tasks = new List<Task>(vmsRunningState.Count);

        for (int i = 0; i < vmsRunningState.Count; i++)
        {
            tasks[i] = new Task(async() => await GetReadyStateOfVM(vmsRunningState.ElementAt(i).Key, currentProxmox));
        }

        await Task.WhenAll(tasks);

        return new Dictionary<int, bool>();
    }

    private async Task<bool> GetReadyStateOfVM(int vmId, ProxmoxModel currentProxmox)
    {
        await Task.Delay(1500);
        return false;
    }
}