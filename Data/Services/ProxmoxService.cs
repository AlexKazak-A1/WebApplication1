using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;

namespace WebApplication1.Data.Services;

public class ProxmoxService : IProxmoxService
{
    private readonly ILogger<ProxmoxService> _logger;

    public readonly IDBService _dbWorker;

    public ProxmoxService(ILogger<ProxmoxService> logger, IDBService provision = null)
    {
        _logger = logger;
        _dbWorker = provision;
    }

    public async Task<Dictionary<int, bool>> StartVmsAsync(List<int> vmIds, int proxmoxId)
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

        var nodeList = (await GetProxmoxNodesListAsync(proxmoxId) as List<ProxmoxNodeInfoDTO>).Select(x => x.Node).ToList();

        string currentTemplateNode = string.Empty;
        for (int i = 0; i < nodeList.Count; i++)
        {
            var vmsOfNode = await GetAllVMsId(proxmoxURL: currentProxmox.ProxmoxURL, nodeName: nodeList[i], accessToken: currentProxmox.ProxmoxToken);
            if (vmsOfNode.Contains(vmIds[0]))
            {
                currentTemplateNode = nodeList[i];
                break;
            }
        }


        var resultVMsState = new Dictionary<int, bool>();

        for (int i = 0; i < vmIds.Count; i++)
        {
            var state = await StartVm(currentProxmox, vmIds[i], currentTemplateNode);
            resultVMsState.Add(vmIds[i], state);
        }

        return resultVMsState;
    }

    public async Task<object> StartProvisioningVMsAsync(CreateVMsDTO vmInfo)
    {
        List<object> results = new();
        if (CheckAllParamsAsync(vmInfo))
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

                    _logger.LogInformation("Created VM = " + result.ToString());
                }

                foreach (var result in workerStatus)
                {

                    results.Add(result);

                    _logger.LogInformation("Created VM = " + result.ToString());
                }
            }
        }
        return results;
    }    

    public async Task<List<ProxmoxNodeInfoDTO>> GetProxmoxNodesListAsync(int proxmoxId)
    {
        var nodesList = new List<ProxmoxNodeInfoDTO>();

        var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();
        if (proxmoxConn == null)
        {
            return nodesList;
        }

        var response = await SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes", HttpMethod.Get, proxmoxConn.ProxmoxToken);

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
            var response = await SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes/{node}/qemu", HttpMethod.Get, proxmoxConn.ProxmoxToken);

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
                        templates.Add(qemu.VmId, $"{qemu.VmId} {qemu.Name}");
                    }
                }
            }
        }

        return templates;
    }

    public async Task<Dictionary<int, bool>> WaitReadyStatusAsync(Dictionary<int, bool> vmsRunningState, int proxmoxId, string connectionString)
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

        foreach (var vm in vmsRunningState)
        {
            Task.Run(async () => await GetReadyStateOfVM(proxmoxId, vm.Key, currentProxmox, connectionString));
        }
        
        var t = 0;

        return new Dictionary<int, bool>();
    }

    public async Task<JsonResult> GetTemplate([FromBody] ProxmoxIdDTO data)
    {
        try
        {
            var options = new List<SelectOptionDTO>();

            var nodesNameList = new List<string>();

            var proxmoxId = data.ProxmoxId;

            var nodesList = await GetProxmoxNodesListAsync(proxmoxId) as List<ProxmoxNodeInfoDTO>;
            nodesNameList.AddRange(nodesList.Select(x => x.Node));
            //var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();
            if (nodesList.Count > 0)
            {
                var templates = await GetAllNodesTemplatesIds(nodesNameList, proxmoxId);

                foreach (var template in templates as Dictionary<int, string>)
                {
                    options.Add(new SelectOptionDTO { Value = template.Key.ToString(), Text = template.Value });
                }
                return new JsonResult(options);
            }
            else
            {
                return new JsonResult(options);
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new List<SelectOptionDTO>());
        }
    }

    public async Task<JsonResult> GetTemplateParams([FromBody] TemplateIdDTO data)
    {
        try
        {
            var proxmoxId = data.ProxmoxId;
            var templateId = data.TemplateId;
            var templateParams = await GetVmInfoAsync(proxmoxId, templateId);

            return new JsonResult(new TemplateParams { 
                CPU = templateParams.CPUS.ToString(),
                RAM = $"{FormatBytes(templateParams.MaxMem):0.0}",
                HDD = $"{FormatBytes(templateParams.MaxDisk):0.0}"
            });
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"check {nameof(GetTemplateParams)} in {nameof(ProxmoxService)}");
        }
    }

    public async Task<JsonResult> CreateNewProxmoxCred([FromBody] ProxmoxModel model)
    {

        if (string.IsNullOrEmpty(model.ProxmoxURL))
        {
            return new JsonResult(new Response { Status = Status.WARNING, Message = "Proxmox url is empty" });
        }

        if (string.IsNullOrEmpty(model.ProxmoxToken))
        {
            return new JsonResult(new Response { Status = Status.WARNING, Message = "Proxmox Token is empty" });
        }

        if (model.ProxmoxNetTags == null || model.ProxmoxNetTags.Length == 0)
        {
            return new JsonResult(new Response { Status = Status.WARNING, Message = "NO Proxmox VLAN TAGS" });
        }
        else
        {
            for (int i = 0; i < model.ProxmoxNetTags.Length; i++)
            {
                if (model.ProxmoxNetTags[i] == string.Empty)
                {
                    return new JsonResult(new Response { Status = Status.WARNING, Message = "One of VLAN TAGS is empty" });
                }
            }
        }

        if (!await _dbWorker.CheckDBConnection())
        {
            return new JsonResult(new Response { Status = Status.ERROR, Message = "Database is unreachable" });
        }

        if (await _dbWorker.AddNewCred(model))
        {
            return new JsonResult(new Response { Status = Status.OK, Message = "New Proxmox Creds was successfully added" });
        }

        return new JsonResult(new Response
        {
            Status = Status.WARNING,
            Message = "New Proxmox Creds wasn`t added.\n" +
            "Maybe you try to add existing data.\n Contact an adnimistrator"
        });
    }

    private async Task<ProxmoxResponse> SendRequestToProxmoxAsync(string url, HttpMethod httpMethod, string token, object? data = null)
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
            return new ProxmoxResponse { Error = $"Error in {nameof(SendRequestToProxmoxAsync)}", Data = null };
        }
    }

    private bool CheckAllParamsAsync(CreateVMsDTO vmInfo)
    {
        if (vmInfo?.EtcdAndControlPlaneAmount > 0 && vmInfo?.ProxmoxId > 0 && vmInfo?.VMTemplateId > 0 &&
            vmInfo?.WorkerAmount > 0 && vmInfo?.RancherId > 0 && vmInfo.ClusterName != null &&
            _dbWorker != null && vmInfo?.VMStartIndex > 0)
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

        string currentTemplateNode = string.Empty;
        for (int i = 0; i < nodesList.Count; i++)
        {
            var vmsOfNode = await GetAllVMsId(proxmoxURL: proxmoxCred.ProxmoxURL, nodeName: nodesList[i], accessToken: proxmoxCred.ProxmoxToken);
            if (vmsOfNode.Contains(vmInfo.VMTemplateId))
            {
                currentTemplateNode = nodesList[i];
                break;
            }
        }

        var data = new FullCloneDTO { Full = true, Name = vmInfo.VMPrefix, VMId = vmInfo.VMTemplateId, NewId = vmInfo.VMStartIndex, Node = currentTemplateNode };

        var vmList = new List<object>();

        switch (elemrntType)
        {
            case ClusterElemrntType.ETCDAndControlPlane:
                {


                    for (var i = 0; i < vmInfo.EtcdAndControlPlaneAmount; i++)
                    {
                        data.Name += "etcd" + i + 1;
                        data.NewId += i;
                        var payload = JsonConvert.SerializeObject(data);

                        vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, vmInfo.VMTemplateId, payload));
                    }

                    return vmList;
                }
            case ClusterElemrntType.Worker:
                {
                    for (var i = 0; i < vmInfo.WorkerAmount; i++)
                    {
                        data.Name += "worker" + i + 1;
                        data.NewId += vmInfo.EtcdAndControlPlaneAmount + i;
                        var payload = JsonConvert.SerializeObject(data);

                        vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, vmInfo.VMTemplateId, payload));
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

            if (nodeVMs.Contains(newVMID))
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

        while (true)
        {
            url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{nodeName}/qemu/{vmId}/status/start";

            response = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken);
            if (response.Data != null)
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

    private async Task<bool> GetReadyStateOfVM(int proxmoxId, int vmId, ProxmoxModel currentProxmox, string connectionString)
    {
        // emplement hare main logic of checking ready state of vm
        var linuxCommand = "journalctl | grep 'cloud-init' | grep 'finished' | grep 'Up'";
        var pidOfProccess = await SendQemuGuestCommand(proxmoxId, vmId, currentProxmox, linuxCommand);
        var IsVmReady = await GetCommandResult(proxmoxId, vmId, currentProxmox, pidOfProccess);

        if (!IsVmReady)
        {
            return false;
        }

        var vmName = (await GetVmInfoAsync(proxmoxId, vmId)).Name;
        if (vmName.Contains("etcd"))
        {
            linuxCommand = connectionString + " --etcd --controlplane";
            pidOfProccess = await SendQemuGuestCommand(proxmoxId, vmId, currentProxmox, linuxCommand);
            var m = await GetCommandResult(proxmoxId, vmId, currentProxmox, pidOfProccess);
        }
        else if (vmName.Contains("worker"))
        {
            linuxCommand = connectionString + " --worker";
            pidOfProccess = await SendQemuGuestCommand(proxmoxId, vmId, currentProxmox, linuxCommand);
            var m = await GetCommandResult(proxmoxId, vmId, currentProxmox, pidOfProccess);
        }

        return true;
    }

    private async Task<VmInfoDTO> GetVmInfoAsync(int proxmoxId, int templateId)
    {
        var nodesList = await GetProxmoxNodesListAsync(proxmoxId) as List<ProxmoxNodeInfoDTO>;
        var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();

        foreach (var node in nodesList.Select(x => x.Node))
        {
            var response = await SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes/{node}/qemu", HttpMethod.Get, proxmoxConn.ProxmoxToken);

            
            if (response != null && response.Data is JArray jArray)
            {
                var qemuList = jArray.ToObject<List<VmInfoDTO>>();

                var info = qemuList.FirstOrDefault(x => x.VmId == templateId);

                if (info == null)
                {
                    continue;
                }

                return info;                
            }
        }

        return new VmInfoDTO();
    }

    private double FormatBytes(long bytes)
    {
        const long OneKB = 1024;
        const long OneMB = OneKB * 1024;
        const long OneGB = OneMB * 1024;

        if (bytes >= OneGB)
        {
            return bytes / (double)OneGB;
        }
        else if (bytes >= OneMB)
        {
            return bytes / (double)OneMB;
        }
        else if (bytes >= OneKB)
        {
            return bytes / (double)OneKB;
        }
        else
        {
            return bytes;
        }
    }

    private async Task<int> SendQemuGuestCommand(int proxmoxId, int vmId, ProxmoxModel currentProxmox,string linuxCommand)
    {        
        string node = await GetNodeName(proxmoxId,vmId);        

        var url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{node}/qemu/{vmId}/agent/exec";
        var command = new QemuGuestCommandDTO {
            Command = "/bin/bash",
            InputData = linuxCommand,
        };

        var payload = JsonConvert.SerializeObject(command);

        var responce = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken, payload);

        var res = JsonConvert.DeserializeObject<QemuGuestCommandResponceDTO>(responce.Data.ToString());

        return res.Pid;
    }

    private async Task<bool> GetCommandResult(int proxmoxId, int vmId, ProxmoxModel currentProxmox, int pid)
    {
        var nodeList = (await GetProxmoxNodesListAsync(proxmoxId)).Select(x => x.Node);
        string node = await GetNodeName(proxmoxId, vmId);

        var url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{node}/qemu/{vmId}/agent/exec-status?pid={pid}";

        while (true)
        {
            var result = (await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken)).Data;
            var res = JsonConvert.DeserializeObject<QemuGuestStatusResponceDTO>(result.ToString());

            if (res != null && res.ExitCode == 0 && res.Exited && !string.IsNullOrEmpty(res.OutPut))
            {
                return true;               
            }

            await Task.Delay(5000);
        }
    }

    private async Task<string> GetNodeName(int proxmoxId, int vmId)
    {
        var nodeList = (await GetProxmoxNodesListAsync(proxmoxId)).Select(x => x.Node);
        var currentProxmox = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();

        foreach (var item in nodeList)
        {
            var url = currentProxmox?.ProxmoxURL + $"/api2/json/nodes/{item}/qemu/{vmId}/config";
            var res = (await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken)).Data;

            if (res != null)
            {
                return item;
            }
        }
        return "";
    }

    //private async Task ConnectVmToRancher()
}