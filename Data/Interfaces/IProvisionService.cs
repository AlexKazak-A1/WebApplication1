using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.Enums;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.WEB;

namespace WebApplication1.Data.Interfaces;

public interface IProvisionService
{
    public Task<JsonResult> GetConnectionCreds([FromBody] ConnectionType connectionTarget);

    public Task<JsonResult> CreateClaster([FromBody] CreateClusterDTO data);

    public Task<JsonResult> CreateProxmoxVMs([FromBody] CreateVMsDTO data);

    public Task<JsonResult> GetConnectionStringToRancher([FromBody] CreateClusterDTO clusterInfo);

    public Task<JsonResult> StartVMAndConnectToRancher([FromBody] ConnectVmToRancherDTO data);

    public Task<JsonResult> GetCreationAvailibility(CreateVMsDTO info);
}
