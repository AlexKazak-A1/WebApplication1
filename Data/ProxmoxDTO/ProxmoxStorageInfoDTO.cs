namespace WebApplication1.Data.ProxmoxDTO;

public class ProxmoxStorageInfoDTO
{
    public string Storage { get; set; }

    public string ResourceGroup { get; set; }

    public string Content { get; set; }

    public string Digest { get; set; }

    public bool Shared { get; set; }

    public string Controller { get; set; }

    public string PrefereLocal { get; set; }

    public string Type { get; set; }

    public string Path { get; set; }

    public string Server { get; set; }

    public string Export { get; set; }

    public string PruneBackups { get; set; }

    public string VGName { get; set; }

    public string Nodes { get; set; }
}
