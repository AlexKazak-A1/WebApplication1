using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data.Database;

public class MainDBContext : DbContext
{
    public MainDBContext(DbContextOptions<MainDBContext> options) : base(options)
    {
        CheckDatabase();
    }

    public DbSet<ProxmoxModel> Proxmox { get; set; }
    public DbSet<RancherModel> Rancher { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProxmoxModel>()
            .Property(p => p.DefaultConfig)
            .HasColumnType("jsonb");
    }

    private async Task CheckDatabase()
    {
        await Database.EnsureCreatedAsync();
    }
}
