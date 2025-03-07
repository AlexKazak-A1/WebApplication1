using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

/// <summary>
/// Describes Rancher for DB
/// </summary>
public class RancherModel
{
    /// <summary>
    /// Uniqur Id for DB record
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Rancher access URL
    /// </summary>
    public string RancherURL { get; set; }

    /// <summary>
    /// Secret of current Rancher user token
    /// </summary>
    public string RancherToken { get; set; }
}
