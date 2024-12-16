using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class Proxmox
{
    private ILogger _logger;

    public int? EtcdAndControlPlaneAmount { get; set; } = null;

    public int? WorkersAmount { get; set; } = null;

    public int? ProxmoxID { get; set; } = null;

    public int? VMTemplateID { get; set; } = null;

    public CreateClusterDTO? Rancher { get; set; } = null;

    public int? VMStartIndex { get; set; } = null;

    public string VMPrefix { get; set; } = string.Empty;

    public IProvision _dbWorker;

    public Proxmox(ILogger logger, IProvision provision = null)
    {
        _logger = logger;
        _dbWorker = provision;
    }

    public async Task<object> StartProvisioningVMsAsync()
    {
        List<string> results = new();
        if(await CheckAllParamsAsync())
        {
            var isETCDAndControlPlaneCreated = await CreateVmOfType(ClusterElemrntType.ETCDAndControlPlane);
            var isWorkerCreated = await CreateVmOfType(ClusterElemrntType.Worker);

            //await Task.WhenAll(isETCDAndControlPlaneCreated, isWorkerCreated);

            var etcdAndCPlaneStatus = isETCDAndControlPlaneCreated as List<object>;

            var workerStatus = isWorkerCreated as List<object>;

            if (etcdAndCPlaneStatus != null && workerStatus != null)
            {
                foreach (var result in etcdAndCPlaneStatus)
                {
                    if ( result is int)
                    {
                        results.Add($"Successfully created VM = {result}");
                    }
                    else
                    {
                        results.Add(result.ToString());
                    }
                    
                    _logger.LogInformation(result.ToString());
                }

                foreach (var result in workerStatus)
                {
                    if (result is int)
                    {
                        results.Add($"Successfully created VM = {result}");
                    }
                    else
                    {
                        results.Add(result.ToString());
                    }

                    _logger.LogInformation(result.ToString());
                }
            }
        }
        return results;
    }

    public async Task<object> SendRequestToProxmoxAsync(string url, HttpMethod httpMethod, string token, object? data = null)
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
                // Create the content with the JSON payload
                var content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");

                request = new HttpRequestMessage(httpMethod, url)
                {
                    Content = content,
                };

            }
            else if (httpMethod == HttpMethod.Get)
            {
                request = new HttpRequestMessage(httpMethod, url);

            }

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Authorization", token);

            var response = await httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            return result;
        }
        catch (Exception ex)
        {            
            _logger.LogError($"{nameof(Proxmox)} \n" + ex.Message);
            return "Error in SendRequestToProxmoxAsync()";
        }
    }

    public async Task<object> GetProxmoxNodesListAsync(int proxmoxId)
    {
        var nodesList = new List<ProxmoxNodeInfoDTO>();

        var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();
        if (proxmoxConn != null)
        {
            var response = await new Proxmox(_logger).SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes", HttpMethod.Get, proxmoxConn.ProxmoxToken);

            if (response != null && response is string result)
            {
                var info = JsonConvert.DeserializeObject<ProxmoxResponse>(result);
                if (info != null && info.Data is JArray jArray)
                {
                    var nodesInfoList = jArray.ToObject<List<ProxmoxNodeInfoDTO>>();

                    if (nodesInfoList != null)
                    {
                        //foreach (var node in nodesInfoList)
                        //{
                        //    if (node.Type == "node")
                        //    {
                        //        nodesList.Add(node.Node);
                        //    }
                        //}

                        nodesList.AddRange(nodesInfoList);
                    }
                }
            }

            return nodesList;
        }

        return nodesList;
    }

    public async Task<object> GetAllNodesTemplatesIds(List<string> nodesName, int proxmoxId)
    {
        var templates = new Dictionary<int, string>();

        var proxmoxConn = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == proxmoxId).FirstOrDefault();

        if (nodesName != null && nodesName.Count > 0 && proxmoxConn != null)
        {
            foreach (var node in nodesName)
            {
                var response = await new Proxmox(_logger).SendRequestToProxmoxAsync($"{proxmoxConn.ProxmoxURL}/api2/json/nodes/{node}/qemu", HttpMethod.Get, proxmoxConn.ProxmoxToken);

                if (response != null && response is string result)
                {
                    var info = JsonConvert.DeserializeObject<ProxmoxResponse>(result);
                    if (info != null && info.Data is JArray jArray)
                    {
                        var qemuList = jArray.ToObject<List<ProxmoxQemuDTO>>();

                        if (qemuList != null)
                        {
                            foreach (var qemu in qemuList)
                            {
                                if (qemu.Template)
                                {
                                    templates.Add(qemu.VmId, qemu.Name);
                                }
                            }
                        }
                    }
                }
            }
        }

        return templates;
    }

    private async Task<bool> CheckAllParamsAsync()
    {
        if (this.EtcdAndControlPlaneAmount != null && this.ProxmoxID != null && this.VMTemplateID != null &&
            this.WorkersAmount != null && this.Rancher?.RancherId != null && this.Rancher.ClasterName != null &&
            this._dbWorker != null && this.VMStartIndex != null)
        {
            if (this.VMPrefix == string.Empty || this.VMPrefix == null)
            {
                this.VMPrefix = "rke2-";
            }
            return true;
        }

        return false;
    }


    /// <summary>
    /// Create VM of specified type. Must be used after CheckAllParamsAsync()
    /// </summary>
    /// <param name="elemrntType">Type from ClusterElemrntType enum</param>
    /// <returns></returns>
    private async Task<object> CreateVmOfType(ClusterElemrntType elemrntType)
    {
        if (_dbWorker == null)
        {
            return false;
        }

        var proxmoxCred = (await _dbWorker.GetConnectionCredsAsync(ConnectionType.Proxmox) as List<ProxmoxModel>).Where(x => x.Id == this.ProxmoxID).FirstOrDefault();

        if (proxmoxCred == null)
        {
            return false;
        }

        var nodesList = (await GetProxmoxNodesListAsync((int)this.ProxmoxID) as List<ProxmoxNodeInfoDTO>).Select(x => x.Node).ToList();

        if (nodesList == null)
        {
            return false;
        }

        var data = new FullCloneDTO { Full = true, Name = this.VMPrefix, VMId = (int)this.VMTemplateID, NewId = (int)VMStartIndex, Node = nodesList[0] };

        var vmList = new List<object>();

        switch (elemrntType)
        {
            case ClusterElemrntType.ETCDAndControlPlane:
                {
                    

                    for (var i = 0; i < this.EtcdAndControlPlaneAmount; i++)
                    {
                        data.Name += "etcd" + i + 1;
                        data.NewId += i;
                        var payload = JsonConvert.SerializeObject(data);

                        vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, nodesList[0], proxmoxCred.ProxmoxToken, payload));          
                    }

                    return vmList;
                }                         
            case ClusterElemrntType.Worker:
                {
                    for (var i = 0; i < this.WorkersAmount; i++)
                    {
                        data.Name += "worker" + i + 1;
                        data.NewId += (int)this.EtcdAndControlPlaneAmount + i;
                        var payload = JsonConvert.SerializeObject(data);                     

                        vmList.Add(await CreateVM(proxmoxCred.ProxmoxURL, nodesList[0], proxmoxCred.ProxmoxToken, payload));
                    }

                    return vmList;
                }                
            default:
                return false;                
        }
    }

    private async Task<object> CreateVM(string proxmoxURL, string nodeName, string accessToken, object payload)
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

            var response = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/qemu/{this.VMTemplateID}/clone", HttpMethod.Post, accessToken, payload);

            var upid = JsonConvert.DeserializeObject<ProxmoxResponse>(response as string).Data.ToString();
            

            while (true)
            {
                var status = await SendRequestToProxmoxAsync($"{proxmoxURL}/api2/json/nodes/{nodeName}/tasks/{upid}/status", HttpMethod.Get, accessToken);
                var result = JsonConvert.DeserializeObject<ProxmoxResponse>(status as string).Data.ToString();
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

        var result = JsonConvert.DeserializeObject<ProxmoxResponse>(response as string);

        if (result != null && result.Data is JArray jArray)
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
}
