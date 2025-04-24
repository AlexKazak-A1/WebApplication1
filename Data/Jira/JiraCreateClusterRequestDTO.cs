using WebApplication1.Data;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;

namespace WebApplication1.Data.Jira;

public class JiraCreateClusterRequestDTO
{
    public string UniqueProxmoxName { get; set; } = string.Empty;

    public string UniqueRancherName { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public int WorkerAmount { get; set; } = default;

    public TemplateParams ParamsPerWorker { get; set; } = default;

    public string ClusterName { get; set; } = default;    
}
