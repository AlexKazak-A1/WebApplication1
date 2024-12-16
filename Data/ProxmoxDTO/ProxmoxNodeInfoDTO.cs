using System.Text.Json.Serialization;

namespace WebApplication1.Data.ProxmoxDTO;

public class ProxmoxNodeInfoDTO
{
    /// <summary>
    /// Full node name "node/{nodeName}"
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Status unknown | online | offline
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Node uptime in seconds.
    /// </summary>
    [JsonPropertyName("uptime")]
    public long Uptime { get; set; }

    /// <summary>
    /// Current amount of used Disk size in bytes
    /// </summary>
    [JsonPropertyName("disk")]
    public long Disk { get; set; }

    /// <summary>
    /// Maximal amount of Disk size in bytes
    /// </summary>
    [JsonPropertyName("maxdisk")]
    public long MaxDisk { get; set; }

    /// <summary>
    /// Used memory in bytes.
    /// </summary>
    [JsonPropertyName("mem")]
    public long Mem { get; set; }

    /// <summary>
    /// Number of available memory in bytes.
    /// </summary>
    [JsonPropertyName("maxmem")]
    public long MaxMem { get; set; }

    /// <summary>
    /// The SSL fingerprint for the node certificate.
    /// </summary>
    [JsonPropertyName("ssl_fingerprint")]
    public string SSL_Fingerprint { get; set; }

    /// <summary>
    /// The cluster node name.
    /// </summary>
    [JsonPropertyName("node")]
    public string Node { get; set; }

    /// <summary>
    /// Type of object (possible node | cluster)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Support level.
    /// </summary>
    [JsonPropertyName("level")]
    public string Level { get; set; }

    /// <summary>
    /// Number of available CPUs.
    /// </summary>
    [JsonPropertyName("maxcpu")]
    public int MaxCpu { get; set; }

    /// <summary>
    /// CPU utilization.
    /// </summary>
    [JsonPropertyName("cpu")]
    public double Cpu { get; set; }
}
