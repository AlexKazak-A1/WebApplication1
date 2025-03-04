﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Configuration;
using WebApplication1.Data.Enums;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;
using WebApplication1.Data.ProxmoxDTO.Node;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Linq;

namespace WebApplication1.Data.Services;

public class ProxmoxService : IProxmoxService
{
    private readonly string MASTER = "master";
    private readonly string WORKER = "worker";
    private object _sync = new object();
    private readonly ILogger<ProxmoxService> _logger;

    private readonly IDBService _dbWorker;

    private readonly IConfiguration _configuration;


    public ProxmoxService(ILogger<ProxmoxService> logger, IConfiguration configuration, IDBService provision = null)
    {
        _logger = logger;
        _dbWorker = provision;
        _configuration = configuration;
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
        var resultVMsState = new Dictionary<int, bool>();

       
        for (int i = 0; i < vmIds.Count; i++)
        {
            for (int j = 0; j < nodeList.Count; j++)
            {
                var vmsOfNode = await GetAllVMsIdAndName(proxmoxURL: currentProxmox.ProxmoxURL, nodeName: nodeList[j], accessToken: currentProxmox.ProxmoxToken);
                if (vmsOfNode.Keys.Contains(vmIds[i]))
                {
                    currentTemplateNode = nodeList[j];
                    break;
                }
            }
            var state = await StartVm(currentProxmox, vmIds[i], currentTemplateNode);
            resultVMsState.Add(vmIds[i], state);
        }
        
        

        return resultVMsState;
    }

    public async Task<object> StartProvisioningVMsAsync(CreateVMsDTO vmInfo)
    {
        List<object> results = new();

        if (!CheckAllParams(vmInfo))
        {
            return results;            
        }

        if (vmInfo.ProvisionSchema == null)
        {
            var etcdCreationStatus = await CreateVmOfType(vmInfo, ClusterElemrntType.ETCDAndControlPlane) as List<object>;
            var workerCreationStatus = await CreateVmOfType(vmInfo, ClusterElemrntType.Worker) as List<object>;

            results.AddRange(ConvertToListString(etcdCreationStatus));
            results.AddRange(ConvertToListString(workerCreationStatus));

            return results;
        }        

        var VmsCreationStatus = await CreateVmOfType(vmInfo) as List<object>;

        results = ConvertToListString(VmsCreationStatus);

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
                    if (qemu.Template && !templates.ContainsValue(qemu.Name))
                    {                        
                        templates.Add(qemu.VmId, $"{qemu.Name}");
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

        //var res = new Dictionary<int, bool>();
        //foreach (var vm in vmsRunningState)
        //{
        //   res.Add(vm.Key, await GetReadyStateOfVM(proxmoxId, vm.Key, currentProxmox, connectionString));
        //}
        //if (res.Count > 0)
        //{           
        //    return res;
        //}

        var res = new Dictionary<int, Task<bool>>();
        foreach (var vm in vmsRunningState)
        {
            res.Add(vm.Key, GetReadyStateOfVM(proxmoxId, vm.Key, currentProxmox, connectionString));
        }

        Task.WaitAll(res.Values.ToArray());
        if (res.Count > 0)
        {
            var result = new Dictionary<int, bool>();
            foreach (var vm in res)
            {
                result.Add(vm.Key, await vm.Value);
            }
            return result;
        }

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

    /// <summary>
    /// Checks an ability to create all vms according to oversubscription
    /// </summary>
    /// <param name="param">Info for creating VMs</param>
    /// <param name="VMsAllocation"></param>
    /// <returns>Boolean True = Available, False = Not</returns>
    public async Task<Dictionary<string, List<string>>?> CheckCreationAbility(CreateVMsDTO param)
    {
        try
        {
            var cpuLimitConfig = _configuration["CPUUsageLimit"] ?? "0.8";
            var oversubConfig = _configuration["OverSubscriptionLimit"] ?? "2.5";
            var CPU_USAGE_LIMIT = double.Parse(cpuLimitConfig, CultureInfo.InvariantCulture);
            var OVERSUBSCRIPTION_LIMIT = double.Parse(oversubConfig, CultureInfo.InvariantCulture);
            var VMsAllocation = new Dictionary<string, List<string>>();

            if (param == null)
            {
                return null;
            }

            if (param.EtcdAndControlPlaneAmount > param.ETCDProvisionRange.Count)
            {
                return null;
            }

            var currentProxmox = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == param.ProxmoxId).FirstOrDefault();

            if (currentProxmox == null)
            {
                return null;
            }

            var allNodesInfo = await GetProxmoxNodesListAsync(param.ProxmoxId);
            var nodeList = allNodesInfo.Select(x => x.Node).ToList();

            if (!nodeList.Any())
            {
                return null;
            }

            var currentOversub = new Dictionary<string, NodeOversubscriptionDTO>();
            foreach (var node in nodeList)
            {
                var nodeOversub = await CountOversubscription(currentProxmox, node, CPU_USAGE_LIMIT);
                currentOversub.Add(node, nodeOversub);
            }

            CheckMinimalRequarements(param); // setting min vm params if values too low, depending on config

            for (int i = 0; i < param.EtcdAndControlPlaneAmount; i++)
            {
                foreach (var node in currentOversub.OrderBy(x => x.Value.CurrentOversubscription))
                {
                    if (!VMsAllocation.ContainsKey(node.Key) && currentOversub[node.Key].CurrentOversubscription < OVERSUBSCRIPTION_LIMIT && param.ETCDProvisionRange.Contains(node.Key))
                    {
                        VMsAllocation.Add(node.Key, new List<string> { MASTER + $"-0{i + 1}" });
                        currentOversub[node.Key].TotalAllocatedCPUs += int.Parse(param.etcdConfig.CPU);
                        break;
                    }
                    else if(VMsAllocation.ContainsKey(node.Key) || !param.ETCDProvisionRange.Contains(node.Key))
                    {
                        continue;
                    }

                        return null;
                }
            }            

            for (int i = 0; i < param.WorkerAmount; i++)
            {
                foreach (var node in currentOversub.OrderBy(x => x.Value.CurrentOversubscription))
                {
                    if (currentOversub[node.Key].CurrentOversubscription < OVERSUBSCRIPTION_LIMIT && param.WorkerProvisionRange.Contains(node.Key))
                    {
                        if (VMsAllocation.ContainsKey(node.Key))
                        {
                            VMsAllocation[node.Key].Add(WORKER + $"-0{i + 1}");
                            currentOversub[node.Key].TotalAllocatedCPUs += int.Parse(param.VMConfig.CPU);
                            break;
                        }
                        else
                        {
                            VMsAllocation.Add(node.Key, new List<string> { WORKER + $"-0{i + 1}" });
                            currentOversub[node.Key].TotalAllocatedCPUs += int.Parse(param.VMConfig.CPU);
                            break;
                        }
                    }
                }
                //var node = currentOversub.Where(x => x.Value.CurrentOversubscription == currentOversub.Min(y => y.Value.CurrentOversubscription)).FirstOrDefault().Key;
                //if (currentOversub[node].CurrentOversubscription < OVERSUBSCRIPTION_LIMIT && param.WorkerProvisionRange.Contains(node))
                //{
                //    if (VMsAllocation.ContainsKey(node))
                //    {
                //        VMsAllocation[node].Add(WORKER + $"-0{i + 1}");
                //        currentOversub[node].TotalAllocatedCPUs += int.Parse(param.VMConfig.CPU);
                //    }
                //    else
                //    {
                //        VMsAllocation.Add(node, new List<string> { WORKER + $"-0{i + 1}" });
                //        currentOversub[node].TotalAllocatedCPUs += int.Parse(param.VMConfig.CPU);
                //    }
                //}
            }

            
            

            var t = 0;
            return VMsAllocation;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Some error in {nameof(ProxmoxService.CheckCreationAbility)}\n" +
                $"{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Returns list of avalable storages in proxmox cluster
    /// </summary>
    /// <param name="proxmoxId"></param>
    /// <returns></returns>
    /// <exception cref="Exception">Check this Method</exception>
    public async Task<List<ProxmoxResourcesDTO>> GetProxmoxStoragesAsync(int proxmoxId)
    {
        try
        {
            var currentProxmox = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();

            if (currentProxmox == null)
            {
                return null;
            }

            var url = $"{currentProxmox.ProxmoxURL}/api2/json/cluster/resources";
            var clusterInfo = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken);

            if (clusterInfo == null || clusterInfo.Data == null)
            {
                return new List<ProxmoxResourcesDTO>();
            }

            var resources = JsonConvert.DeserializeObject<List<ProxmoxResourcesDTO>>(clusterInfo.Data.ToString());



            return resources.Where(x => x.Type == "storage").ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message + "\n" + ex.InnerException?.Message);
            return new List<ProxmoxResourcesDTO>();
        }
    }

    private async Task<NodeOversubscriptionDTO> CountOversubscription(ProxmoxModel currentProx, string nodeName, double cpuLimit)
    {
        var url = $"{currentProx.ProxmoxURL}/api2/json/nodes/{nodeName}/status";
        var clusterStatus = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProx.ProxmoxToken);

        if (clusterStatus == null)
        {
            return new NodeOversubscriptionDTO { TotalAllocatedCPUs = 6, TotalNodeCPU = 2 };
        }

        var nodeData = JsonConvert.DeserializeObject<ProxmoxNodeStatusDTO>(clusterStatus?.Data?.ToString()!);

        if (nodeData?.CPU > cpuLimit)
        {
            return new NodeOversubscriptionDTO { TotalAllocatedCPUs = 6, TotalNodeCPU = 2 };
        }

        var totalCpus = 0;
        var allVms = await GetAllNodeVMsInfo(currentProx.ProxmoxURL, nodeName, currentProx.ProxmoxToken);

        foreach (var vm in allVms)
        {
            if (!vm.Template)
            {
                totalCpus += vm.CPUs;
            }                
        }

        return new NodeOversubscriptionDTO { TotalNodeCPU = nodeData!.CPUInfo.CPUs, TotalAllocatedCPUs = totalCpus };        
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
            if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
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

    private bool CheckAllParams(CreateVMsDTO vmInfo)
    {
        if (vmInfo?.EtcdAndControlPlaneAmount > 0 && vmInfo?.ProxmoxId > 0 && vmInfo?.VMTemplateName.Length > 0 &&
            vmInfo?.WorkerAmount > 0 && vmInfo?.RancherId > 0 && vmInfo.ClusterName != null &&
            _dbWorker != null && vmInfo?.VMStartIndex > 0)
        {
            if (vmInfo.VMPrefix == string.Empty || vmInfo.VMPrefix == null)
            {
                vmInfo.VMPrefix = "rke2-";
            }

            CheckMinimalRequarements(vmInfo);
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
    private async Task<object> CreateVmOfType(CreateVMsDTO vmInfo, ClusterElemrntType elemrntType = default)
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
        int currentTemplateId = 0;

        var data = new FullCloneDTO { Full = true, Name = vmInfo.VMPrefix, VMId = currentTemplateId, NewId = vmInfo.VMStartIndex, Node = currentTemplateNode, Storage = "test" };

        var vmList = new List<object>();

        if (vmInfo.ProvisionSchema != null)
        {
            var counter = 0;
            var newIdIndex = data.NewId;
            foreach (var vmProvition in vmInfo.ProvisionSchema)
            {
                var currentNodeVMs = await GetAllVMsIdAndName(proxmoxURL: proxmoxCred.ProxmoxURL, nodeName: vmProvition.Key, accessToken: proxmoxCred.ProxmoxToken);
                currentTemplateId = currentNodeVMs.Where(x => x.Value.Equals(vmInfo.VMTemplateName)).FirstOrDefault().Key;
                foreach (var vmName in vmProvition.Value)
                {
                    data.Name = vmInfo.ClusterName + '-' + vmName;
                    data.NewId = newIdIndex + counter++;
                    data.Node = vmProvition.Key;
                    data.VMId = currentTemplateId;
                    data.Storage = await SelectMaxStorageSize(vmInfo.SelectedStorage, vmInfo.ProxmoxId); 
                    currentTemplateNode = vmProvition.Key;
                    var payload = JsonConvert.SerializeObject(data);

                    

                    //Task.Run(async () => 
                    //{
                    //    vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));

                    //    await Reconfigure(vmInfo.ProxmoxId, proxmoxCred, currentTemplateNode, data.NewId, vmInfo.etcdConfig);
                    //});
                    vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));

                    await Reconfigure(vmInfo.ProxmoxId, proxmoxCred, currentTemplateNode, data.NewId, vmInfo.etcdConfig);
                }
            }

            return vmList;

            //switch (elemrntType)
            //{
            //    case ClusterElemrntType.ETCDAndControlPlane:
            //        {
            //            var selectedMasters = vmInfo.ProvisionSchema.Where(x => x.Value.Any(y => y.Contains(MASTER)));

            //            foreach (var node in selectedMasters.Select((value, i) => (value , i)))
            //            {
            //                var name = vmInfo.ClusterName + '-' + node.value.Value.First(x => x.Contains(MASTER));
            //                data.Name = name;
            //                data.NewId += node.i;
            //                data.Node = node.value.Key;
            //                var payload = JsonConvert.SerializeObject(data);

            //                // vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));

            //                // await Reconfigure(vmInfo.ProxmoxId, proxmoxCred, currentTemplateNode, data.NewId, vmInfo.etcdConfig);
            //                continue;
            //            }
                       
            //            return vmList;
            //        }
            //    case ClusterElemrntType.Worker:
            //        {
            //            var selectedWorkers= vmInfo.ProvisionSchema.Where(x => x.Value.Contains(WORKER));
            //            int counter = vmInfo.ProvisionSchema.Count(x => x.Value.Contains(MASTER));
            //            foreach (var node in selectedWorkers.Select((value, i) => (value, i)))
            //            {
            //                foreach (var vms in node.value.Value.Select((vm, i) => (vm, i)))
            //                {
            //                    data.Name += '-' + vms.vm;
            //                    data.NewId += node.i + counter++;
            //                    var payload = JsonConvert.SerializeObject(data);
            //                }
                            

            //               // vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));

            //               // await Reconfigure(vmInfo.ProxmoxId, proxmoxCred, currentTemplateNode, data.NewId, vmInfo.etcdConfig);
            //            }

            //            for (var i = 0; i < vmInfo.WorkerAmount; i++)
            //            {
            //                data.Name += "-worker-" + i + 1;
            //                data.NewId += vmInfo.EtcdAndControlPlaneAmount + i;
            //                var payload = JsonConvert.SerializeObject(data);

            //                vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));

            //                await Reconfigure(vmInfo.ProxmoxId, proxmoxCred, currentTemplateNode, data.NewId, vmInfo.VMConfig);
            //            }

            //            return vmList;
            //        }
            //    default:
            //        return false;
            //}
        }
        else
        {
            switch (elemrntType)
            {
                case ClusterElemrntType.ETCDAndControlPlane:
                    {
                        var t = vmInfo.ProvisionSchema.Where(x => x.Value.Contains(MASTER));

                        for (var i = 0; i < vmInfo.EtcdAndControlPlaneAmount; i++)
                        {
                            data.Name += "-master-" + i + 1;
                            data.NewId += i;
                            var payload = JsonConvert.SerializeObject(data);

                            vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));
                        }

                        return vmList;
                    }
                case ClusterElemrntType.Worker:
                    {
                        for (var i = 0; i < vmInfo.WorkerAmount; i++)
                        {
                            data.Name += "-worker-" + i + 1;
                            data.NewId += vmInfo.EtcdAndControlPlaneAmount + i;
                            var payload = JsonConvert.SerializeObject(data);

                            vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, currentTemplateNode, proxmoxCred.ProxmoxToken, currentTemplateId, payload));

                            await Reconfigure(vmInfo.ProxmoxId, proxmoxCred, currentTemplateNode, data.NewId, vmInfo.VMConfig);
                        }

                        return vmList;
                    }
                default:
                    return false;
            }
        }            
    }

    private async Task<string> SelectMaxStorageSize(List<string> storageSource, int proxmoxId)
    {
        var allStorages = await GetProxmoxStoragesAsync(proxmoxId);
        var maxStorageSizeName = allStorages.OrderBy(x => x.MaxDisk - x.Disk).Select(x => x.Storage).Intersect(storageSource).FirstOrDefault();
        return maxStorageSizeName ?? string.Empty;
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
            var nodeVMs = await GetAllVMsIdAndName(proxmoxURL, nodeName, accessToken);

            if (nodeVMs.Keys.Contains(newVMID))
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

    private async Task<Dictionary<int,string>> GetAllVMsIdAndName(string proxmoxURL, string nodeName, string accessToken)
    {
        var vmList = new Dictionary<int, string>();

        var response = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/qemu/", HttpMethod.Get, accessToken);

        if (response != null && response.Data is JArray jArray)
        {
            var qemuList = jArray.ToObject<List<ProxmoxQemuDTO>>();

            if (qemuList != null)
            {
                foreach (var qemu in qemuList)
                {
                    vmList.Add(qemu.VmId,qemu.Name);
                }
            }
        }

        return vmList;
    }

    private async Task<List<ProxmoxQemuDTO>> GetAllNodeVMsInfo(string proxmoxURL, string nodeName, string accessToken)
    {
        var response = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/qemu/", HttpMethod.Get, accessToken);

        if (response != null && response.Data is JArray jArray)
        {
            var qemuList = jArray.ToObject<List<ProxmoxQemuDTO>>();

            if (qemuList != null)
            {
                return qemuList;
            }
        }

        return new List<ProxmoxQemuDTO>();
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
        var linuxCommand = "journalctl | grep 'cloud-init' | grep 'finished' | grep 'Up'";
        var IsVmReady = await SendQemuGuestCommand(proxmoxId, vmId, currentProxmox, linuxCommand);
        


        if (!IsVmReady)
        {
            return false;
        }

        var listOfCommands = new List<string> 
        {
            "sudo apt update -y",
            "sudo apt upgrade -y",
            //"sudo dpkg --configure -a",
            "sudo apt install linux-generic nfs-common net-tools -y",
            "sudo reboot",
            
        };

        if (!await SendGroupOfCommands(proxmoxId, vmId, currentProxmox, listOfCommands)) 
        {           
            return false;
        }

        var vmName = (await GetVmInfoAsync(proxmoxId, vmId)).Name;
        bool vmReady = false;
        if (vmName.Contains("master") )
        {
            linuxCommand = SetLinuxConnectCommand(connectionString, " --etcd --controlplane");
            vmReady = await SendQemuGuestCommand(proxmoxId, vmId, currentProxmox, linuxCommand);            
        }
        else if (vmName.Contains("worker"))
        {
            linuxCommand = SetLinuxConnectCommand(connectionString, " --worker");
            vmReady = await SendQemuGuestCommand(proxmoxId, vmId, currentProxmox, linuxCommand);            
        }

        return vmReady;
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

    private async Task<bool> SendQemuGuestCommand(int proxmoxId, int vmId, ProxmoxModel currentProxmox,string linuxCommand)
    {        
        try
        {
            string node = string.Empty;
            lock (_sync)
            {
                node = GetNodeName(proxmoxId, vmId).Result;
            }


            var url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{node}/qemu/{vmId}/agent/exec";
            var command = new QemuGuestCommandDTO
            {
                Command = "/bin/bash",
                InputData = linuxCommand,
            };

            var payload = JsonConvert.SerializeObject(command);

            var responce = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken, payload);

            if (command.InputData.Contains("reboot"))
            {
                return true;
            }

            while (responce.Data == null)
            {
                responce = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken, payload);
            }

            var pid = JsonConvert.DeserializeObject<QemuGuestCommandResponceDTO>(responce.Data?.ToString()!)?.Pid ?? 0;

            var result = await GetCommandResult(proxmoxId, vmId, currentProxmox, pid);

            while (result == null)
            {
                responce = await SendRequestToProxmoxAsync(url, HttpMethod.Post, currentProxmox.ProxmoxToken, payload);
                pid = JsonConvert.DeserializeObject<QemuGuestCommandResponceDTO>(responce.Data?.ToString()!)?.Pid ?? 0;
                result = await GetCommandResult(proxmoxId, vmId, currentProxmox, pid);
            }

            return (bool)result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message + "\n" + ex.InnerException?.Message);
            return false;
        }




    }

    private async Task<bool?> GetCommandResult(int proxmoxId, int vmId, ProxmoxModel currentProxmox, int pid)
    {
        try
        {
            var nodeList = (await GetProxmoxNodesListAsync(proxmoxId)).Select(x => x.Node);
            string node = await GetNodeName(proxmoxId, vmId);

            var url = currentProxmox.ProxmoxURL + $"/api2/json/nodes/{node}/qemu/{vmId}/agent/exec-status?pid={pid}";

            while (true)
            {
                var result = await SendRequestToProxmoxAsync(url, HttpMethod.Get, currentProxmox.ProxmoxToken);
                var resultData = result.Data;                

                if (result.ErrorData != null)
                {
                    break;
                }

                var res = JsonConvert.DeserializeObject<QemuGuestStatusResponceDTO>(result.Data?.ToString());

                if (res != null && res.ExitCode == 0 && res.Exited && !string.IsNullOrEmpty(res.OutPut))
                {
                    return true;
                }
                else if (res != null && !res.Exited)
                {
                    await Task.Delay(5000);
                    continue;
                }
                else if (res != null && res.Exited && res.ExitCode == 1)
                {
                    await Task.Delay(5000);
                    return null;
                }
                else if (res != null && res.Exited && res.ExitCode == 100)
                {
                    await SendQemuGuestCommand(proxmoxId, vmId, currentProxmox, "sudo dpkg --configure -a");
                }
                else
                {
                    _logger.LogError($"Error in {vmId}, Error code = {res.ExitCode}");
                    return false;
                }
            }

            _logger.LogError($"Error in {vmId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in {vmId}\n{ex.Message}");
            return false;
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

    private string SetLinuxConnectCommand(string connectionString, string typeConnestionTo)
    {
        var http_proxy = _configuration["HTTP_PROXY"] ?? "http://10.254.49.150:3128";
        var https_proxy = _configuration["HTTPS_PROXY"] ?? "http://10.254.49.150:3128";
        var no_proxy = _configuration["NO_PROXY"] ?? "127.0.0.0/8,10.0.0.0/8,172.16.0.0/12,192.168.0.0/16,.svc,.cluster.local,rancher.a1by.tech,.main.velcom.by";

        connectionString = connectionString.Replace("sudo sh", $"sudo HTTP_PROXY=\"{http_proxy}\" HTTPS_PROXY=\"{https_proxy}\" NO_PROXY=\"{no_proxy}\" sh");
        var result = connectionString + typeConnestionTo;
        return result;
    }

    private async Task Reconfigure(int proxmoxId, ProxmoxModel proxmoxModel, string currentNode, int vmId, TemplateParams vmConfig)
    {
        try
        {
            var info = await GetVmInfoAsync(proxmoxId, vmId);
            var cpu = int.Parse(vmConfig.CPU);
            var templateParam = double.Parse(vmConfig.HDD);
            var currHDDSize = double.Round(FormatBytes(info.MaxDisk), 1);

            if (info.CPUS != cpu || currHDDSize != templateParam)
            {
                // setting VM Params 
                var newConfig = new { sockets = 1, cores = cpu, vcpus = cpu, memory = double.Parse(vmConfig.RAM, CultureInfo.InvariantCulture) * 1024 };
                var payload = JsonConvert.SerializeObject(newConfig);

                var url = $"{proxmoxModel.ProxmoxURL}/api2/json/nodes/{currentNode}/qemu/{vmId}/config";
                await SendRequestToProxmoxAsync(url, HttpMethod.Post, proxmoxModel.ProxmoxToken, payload);


                // Setting New Vm Disk size
                var incrementSize = double.Parse(vmConfig.HDD) - double.Round( FormatBytes(info.MaxDisk), 1);
                
                if (incrementSize == 0)
                {
                    return;
                }

                var newSize = new { Disk = "scsi0", Size = $"+{incrementSize}G" };
                payload = JsonConvert.SerializeObject(newSize);

                url = $"{proxmoxModel.ProxmoxURL}/api2/json/nodes/{currentNode}/qemu/{vmId}/resize";
                 await SendRequestToProxmoxAsync(url, HttpMethod.Put, proxmoxModel.ProxmoxToken, payload);
            }
        }
        catch (Exception ex)
        {

        }
    }

    private async Task<bool> SendGroupOfCommands(int proxmoxId, int vmId, ProxmoxModel currentProxmox, List<string> list)
    {
        var resultList = new List<bool>();
        foreach (var command in list)
        {
            resultList.Add(await SendQemuGuestCommand(proxmoxId, vmId, currentProxmox, command));

            if (command.Contains("reboot"))
            {                
                await Task.Delay(1 * 60 * 1_000);
            }
        }

        if (resultList.Contains(false))
        {
            return false;
        }

        return true;
    }

    private void CheckMinimalRequarements(CreateVMsDTO clusterInfo)
    {
        int.TryParse(_configuration["CPUMin"] ?? "2", out int minCPU);
        double.TryParse(_configuration["RAMMin"] ?? "4", out double minRAM);

        var workerCPU = int.Parse(clusterInfo.VMConfig.CPU, CultureInfo.InvariantCulture);
        var workerRAM = double.Parse(clusterInfo.VMConfig.RAM.Replace(',', '.'), CultureInfo.InvariantCulture);

        var etcdCPU = int.Parse(clusterInfo.etcdConfig.CPU, CultureInfo.InvariantCulture);
        var etcdRAM = double.Parse(clusterInfo.etcdConfig.RAM.Replace(',', '.'), CultureInfo.InvariantCulture);

        if (workerCPU < minCPU)
        {
            clusterInfo.VMConfig.CPU = _configuration["CPUMin"] ?? "2";
        }

        if (etcdCPU < minCPU)
        {
            clusterInfo.etcdConfig.CPU = _configuration["CPUMin"] ?? "2";
        }

        if (workerRAM < minRAM)
        {
            clusterInfo.VMConfig.RAM = _configuration["RAMMin"] ?? "4";
        }

        if (workerRAM < minRAM)
        {
            clusterInfo.etcdConfig.RAM = _configuration["RAMMin"] ?? "4";
        }
    }

    private List<object> ConvertToListString(List<object>? source)
    {
        var results = new List<object>();

        if (source == null)
        {
            return new List<object>();
        }

        foreach (var result in source)
        {

            results.Add(result);

            _logger.LogInformation("Created VM = " + result.ToString());
        }

        return results;
    }

    private async Task<string> GetMaxStorage(ProxmoxModel model)
    {
        var url = $"{model.ProxmoxURL}/api2/json/storage";
        var res = (await SendRequestToProxmoxAsync(url, HttpMethod.Get, model.ProxmoxToken)).Data as List<ProxmoxStorageInfoDTO>;

        return "";
    }    
}
