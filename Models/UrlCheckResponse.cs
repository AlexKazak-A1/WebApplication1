namespace WebApplication1.Models;

/// <summary>
/// Responce
/// </summary>
public class UrlCheckResponse
{
    /// <summary>
    /// Boolean value of connection availability
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Additional info about connection
    /// </summary>
    public string Message { get; set; }
}
