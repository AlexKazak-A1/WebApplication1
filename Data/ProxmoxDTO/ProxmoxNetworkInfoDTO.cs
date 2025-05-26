using Newtonsoft.Json;

namespace WebApplication1.Data.ProxmoxDTO;

#pragma warning disable CS1591
/// <summary>
/// В классе хранится описание всех сетевых параметров доступных для Proxmox хоста
/// </summary>
public class ProxmoxNetworkInfoDTO
{
    public string? MTU { get; set; } = default;

    public string? Netmask { get; set; } = default;

    public string? VlanRawDevice { get; set; } = default;

    public string? Type { get; set; } = default;

    public bool Autostart { get; set; } = default;

    public string? Iface { get; set; } = default;

    public string? Method { get; set; } = default;

    public string? Method6 { get; set; } = default;

    public List<string>? Families { get; set; } = default;

    [JsonProperty("vlan-id")]
    public string? VlanId { get; set; } = default;

    public bool? Active { get; set; } = default;

    public int? Priority { get; set; } = default;

    public string? CIDR { get; set; } = default;

    public string? Gateway { get; set; } = default;

    public string? Address { get; set; } = default;

    public bool? Exists { get; set; } = default;

    public string? BondMiimon { get; set; } = default;

    public string? BondMode { get; set; } = default;

    public string? Slaves { get; set; } = default;

    public string? BondPrimary { get; set; } = default;

    public bool? BridgeVlanAware { get; set; } = default;

    public string? BridgeFd { get; set; } = default;

    public string? BridgeVIDS { get; set; } = default;

    public string? BridgePorts { get; set; } = default;

    public string? BrigdeSTP { get; set; } = default;
}

#pragma warning restore
