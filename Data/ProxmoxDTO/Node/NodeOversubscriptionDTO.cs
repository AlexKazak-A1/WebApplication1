namespace WebApplication1.Data.ProxmoxDTO.Node;

public class NodeOversubscriptionDTO
{
    public int TotalNodeCPU { get; set; } = 0;

    public int TotalAllocatedCPUs { get; set; } = 0;

    public double CurrentOversubscription 
    {
        get
        {
            return (double)this.TotalAllocatedCPUs / 2 / (double)TotalNodeCPU;
        }
    }
}
