using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data.Database;

public class MainDBContext : DbContext
{
    public MainDBContext(DbContextOptions<MainDBContext> options) : base(options)
    {
    }

    public DbSet<ProxmoxModel> Proxmox { get; set; }
    public DbSet<RancherModel> Rancher { get; set; }
}
