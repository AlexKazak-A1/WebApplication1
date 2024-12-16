using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Primitives;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class ProxmoxModel
{
    [Key]
    public int Id { get; set; }
    public string ProxmoxURL {  get; set; }
    public string ProxmoxToken { get; set; }   
    public string[] ProxmoxNetTags {  get; set; } 
}
