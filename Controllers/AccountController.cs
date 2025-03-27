using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Models; // Пространство имен для модели LoginModel

namespace WebApplication1.Controllers;
public class AccountController : Controller
{
    private readonly IConfiguration _configuration;
    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    //[HttpGet]
    //public IActionResult Login()
    //{
    //    return View();
    //}

    [HttpGet]
    public IActionResult Login()
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (ModelState.IsValid)
        {
            // Здесь проверка логина и пароля (заглушка)
            if (model.Username == _configuration["Login"] && model.Password == _configuration["Password"])
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe, // Если пользователь выбрал "Запомнить меня"
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(30) // Таймаут 30 минут
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Неверный логин или пароль.");
        }
        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}

