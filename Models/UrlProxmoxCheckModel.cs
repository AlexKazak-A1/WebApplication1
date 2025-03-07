namespace WebApplication1.Models;

/// <summary>
/// Info for checking connection to Proxomox
/// </summary>
public class UrlProxmoxCheckModel
{
    /// <summary>
    /// Url for Proxmox cluster/host
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Username for connection
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Token Id for connection
    /// </summary>
    public string TokenID { get; set; }

    /// <summary>
    /// Secret of Token ID
    /// </summary>
    public string TokenSecret { get; set; }
}
