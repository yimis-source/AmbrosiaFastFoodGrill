using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ambrosia.Web.Controllers;

/// <summary>Inicio y cierre de sesión con ASP.NET Core Identity (SignInManager).</summary>
[AllowAnonymous]
public class CuentaController : Controller
{
    private readonly SignInManager<IdentityUser> _inicioSesion;
    private readonly UserManager<IdentityUser> _usuarios;

    public CuentaController(SignInManager<IdentityUser> inicioSesion, UserManager<IdentityUser> usuarios)
    {
        _inicioSesion = inicioSesion;
        _usuarios = usuarios;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var nombre = modelo.Usuario.Trim();
        var resultado = await _inicioSesion.PasswordSignInAsync(nombre, modelo.Clave, modelo.Recordarme, lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            if (!string.IsNullOrEmpty(modelo.ReturnUrl) && Url.IsLocalUrl(modelo.ReturnUrl))
                return LocalRedirect(modelo.ReturnUrl);

            // Cada rol aterriza en su pantalla principal.
            var usuario = await _usuarios.FindByNameAsync(nombre);
            var roles = usuario is null ? new List<string>() : await _usuarios.GetRolesAsync(usuario);
            return roles switch
            {
                _ when roles.Contains(Roles.Administrador) => RedirectToAction("Index", "Reportes"),
                _ when roles.Contains(Roles.Cajero) => RedirectToAction("Crear", "Pedidos"),
                _ when roles.Contains(Roles.Cocina) => RedirectToAction("Index", "Cocina"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        ModelState.AddModelError(string.Empty, resultado.IsLockedOut
            ? "La cuenta quedó bloqueada 5 minutos por varios intentos fallidos."
            : "Usuario o contraseña incorrectos.");
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await _inicioSesion.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccesoDenegado() => View();
}
