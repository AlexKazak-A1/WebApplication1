namespace WebApplication1.Data.Enums;

public enum Status
{
    INFO = 100,
    OK = 200,
    REDIRECT = 300,
    WARNING = 400,
    ALREADY_EXIST = 409,
    ERROR = 500
}

/// <summary>
/// Enum of available Connection types
/// </summary>
public enum ConnectionType
{
    /// <summary>
    /// Rancher = 0
    /// </summary>
    Rancher,

    /// <summary>
    /// Proxmox = 1
    /// </summary>
    Proxmox
}

public enum ClusterElemrntType
{
    ETCDAndControlPlane,
    Worker
}
