using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Models;

namespace WebApplication1.Data.Interfaces;

public interface IProxmoxService
{
    public Task<List<ProxmoxNodeInfoDTO>> GetProxmoxNodesListAsync(int proxmoxId);

    public Task<object> GetAllNodesTemplatesIds(List<string> nodesName, int proxmoxId);

    public Task<object> StartProvisioningVMsAsync(CreateVMsDTO vmInfo);

    public Task<Dictionary<int, bool>> StartVmsAsync(List<int> vmIds, int proxmoxId);

    public Task<Dictionary<int, bool>> WaitReadyStatusAsync(Dictionary<int, bool> vmsRunningState, int proxmoxId, string connectionstring);

    public Task<JsonResult> GetTemplate([FromBody] ProxmoxIdDTO data = null);

    public Task<JsonResult> GetTemplateParams([FromBody] TemplateIdDTO data);

    Task<JsonResult> CreateNewProxmoxCred([FromBody] ProxmoxModel model);

    /// <summary>
    /// Checks an ability to create all vms according to oversubscription
    /// </summary>
    /// <param name="param">Info for creating VMs</param>
    /// <returns>Boolean True = Available, False = Not</returns>
    /// <out>Dictionary that specify VMs Allocation</out>
    public Task<Dictionary<string, List<string>>?> CheckCreationAbility(CreateVMsDTO param);

    /// <summary>
    /// Gets the list of all available storages in proxmox cluster
    /// </summary>
    /// <param name="proxmoxId"> Id of Proxmox Cluster from DB.</param>
    /// <returns></returns>
    public Task<List<ProxmoxResourcesDTO>> GetProxmoxStoragesAsync(int proxmoxId);
}
