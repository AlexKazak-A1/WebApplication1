using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Data.WEB;
using WebApplication1.Models;

namespace WebApplication1.Data.Interfaces;

public interface IRancherService
{
    public Task<JsonResult> CreateNewRancherCred([FromBody] RancherModel model);

    public Task<string> GetConnectionString(string RancherId, string ClusterName);

    public Task<JsonResult> CreateClusterAsync(CreateClusterDTO clusterInfo);

    public Task<int> GetRancherCred(string uniqueRancherName);

    public Task<List<RancherModel>> GetAllRancher();

    public Task<bool> UpdateRancher([FromBody] RancherReconfigDTO reconfig);
}
