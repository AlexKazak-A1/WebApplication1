namespace WebApplication1.Data.ProxmoxDTO
{
    /// <summary>
    /// Описывает ресурсы доступные для текущего Proxmox кластера
    /// </summary>
    public class ProxmoxResourcesDTO
    {
        public string Type { get; set; } = string.Empty;

        public double CPU { get; set; } = default;

        public long MaxMem { get; set; } = default;

        public long Mem { get; set; } = default;

        public string Id { get; set; } = string.Empty;

        public int UpTime { get; set; } = default;

        public string Pool { get; set; } = string.Empty;

        public int MaxCPU { get; set; } = default;

        public string Status { get; set; } = "offline";

        public long NetIn { get; set; } = default;

        public long NetOut { get; set; } = default;

        public long Disk { get; set; } = default;

        public long MaxDisk { get; set; } = default;

        public long DiskWrite { get; set; } = default;

        public long DiskRead { get; set; } = default;

        public string Node { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int VmId { get; set; } = default;

        public bool Template { get; set; } = default;

        public string Hastate { get; set; } = string.Empty;

        public string SDN { get; set; } = string.Empty;

        public string PluginType { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public bool Shared { get; set; } = default;

        public string Storage { get; set; } = string.Empty;
    }
}
