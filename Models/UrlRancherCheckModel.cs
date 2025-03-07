namespace WebApplication1.Models;

/// <summary>
/// Info for Checkin connection to Rancher
/// </summary>
public class UrlRancherCheckModel
{
    /// <summary>
    /// Url of Rancher
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Secret token of user
    /// </summary>
    public string Token { get; set; }
}
