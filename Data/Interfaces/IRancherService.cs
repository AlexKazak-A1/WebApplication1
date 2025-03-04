using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data.RancherDTO;
using WebApplication1.Models;

namespace WebApplication1.Data.Interfaces;

public interface IRancherService
{
    public Task<JsonResult> CreateNewRancherCred([FromBody] RancherModel model);
    public Task<string> GetConnectionString(string RancherId, string ClusterName);

    public Task<JsonResult> CreateClusterAsync(CreateClusterDTO clusterInfo);
}
