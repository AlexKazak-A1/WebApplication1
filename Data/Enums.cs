namespace WebApplication1.Data;

public enum Status
{
    INFO = 100,
    OK = 200,
    REDIRECT = 300,
    WARNING = 400,
    ALREADY_EXIST = 409,
    ERROR = 500
}

public enum ConnectionType
{
    Rancher,
    Proxmox
}

public enum ClusterElemrntType
{
    ETCDAndControlPlane,
    Worker
}
