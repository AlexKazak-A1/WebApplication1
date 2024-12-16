using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class RancherModel
{
    [Key]
    public int Id { get; set; }
    public string RancherURL { get; set; }
    public string RancherToken { get; set; }
}
