using WebApplication1.Data.Enums;

namespace WebApplication1.Data.WEB;

/// <summary>
/// Describes resultes of methosd for user
/// </summary>
public class Response
{
    /// <summary>
    /// Curent Status of object(s) in response
    /// </summary>
    public Status Status { get; set; }

    /// <summary>
    /// Text message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// (Optional) if complex data returned
    /// </summary>
    public object? Data { get; set; }
}
