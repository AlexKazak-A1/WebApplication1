using WebApplication1.Data;
using WebApplication1.Data.ProxmoxDTO;
using WebApplication1.Data.RancherDTO;

namespace WebApplication1.Data.Jira;

public class JiraCreateClusterRequestDTO
{
    public CreateVMsDTO CreateVMsDTO { get; set; } = default;

    public CreateClusterDTO CreateClusterDTO { get; set; } = default;    
}
