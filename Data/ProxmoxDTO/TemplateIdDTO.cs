namespace WebApplication1.Data.ProxmoxDTO;

/// <summary>
/// Info about template on Proxmox host
/// </summary>
public class TemplateIdDTO
{
    /// <summary>
    /// Id of Proxmox cluster/host from DB
    /// </summary>
    public int ProxmoxId { get; set; }

    /// <summary>
    /// Id of tamplate on this host
    /// </summary>
    public int TemplateId { get; set; }
}
