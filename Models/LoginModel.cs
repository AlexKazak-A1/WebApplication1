using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Введите имя пользователя")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}