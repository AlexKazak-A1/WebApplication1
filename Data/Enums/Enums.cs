namespace WebApplication1.Data.Enums;

/// <summary>
/// Описывает все доступные статусы сообщений
/// </summary>
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

/// <summary>
/// Тип части кластера для развёртывания
/// </summary>
public enum ClusterElemrntType
{
    /// <summary>
    /// описывает управляющие узлы
    /// </summary>
    ETCDAndControlPlane,

    /// <summary>
    /// описывает рабочие узлы
    /// </summary>
    Worker
}
