namespace WebApplication1.Data.ProxmoxDTO.Node;

/// <summary>
/// Описывает переподписку на конкретном хосте Proxmox
/// </summary>
public class NodeOversubscriptionDTO
{
    /// <summary>
    /// Общее колличество ядер на хосте ичитывая Hyperthreading
    /// </summary>
    public int TotalNodeCPU { get; set; } = 0;

    /// <summary>
    /// Общее колличество выделенных ядев на хосте
    /// </summary>
    public int TotalAllocatedCPUs { get; set; } = 0;

    /// <summary>
    /// Описывает текущую переподписку 
    /// </summary>
    public double CurrentOversubscription 
    {
        get
        {
            return (double)this.TotalAllocatedCPUs / 2 / (double)TotalNodeCPU;
        }
    }
}
