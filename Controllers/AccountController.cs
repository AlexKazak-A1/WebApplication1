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

    [HttpGet]
    public IActionResult Login()
    {
        //return Challenge(new AuthenticationProperties { RedirectUri = "/" }, OpenIdConnectDefaults.AuthenticationScheme);
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, "oidc");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Удаляем куки аутентификации (OpenID + Cookies)
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        //await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        //await HttpContext.SignOutAsync("oidc");

        return RedirectToAction("Index", "Home");
    }
}

