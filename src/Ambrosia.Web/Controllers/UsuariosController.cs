using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ambrosia.Web.Controllers;

/// <summary>El administrador crea las cuentas del personal y les asigna un rol (UserManager de Identity).</summary>
[Authorize(Roles = Roles.Administrador)]
public class UsuariosController : Controller
{
    private readonly UserManager<IdentityUser> _usuarios;

    public UsuariosController(UserManager<IdentityUser> usuarios) => _usuarios = usuarios;

    public async Task<IActionResult> Index()
    {
        var filas = new List<UsuarioFilaViewModel>();
        foreach (var usuario in _usuarios.Users.OrderBy(u => u.UserName).ToList())
        {
            filas.Add(new UsuarioFilaViewModel
            {
                Id = usuario.Id,
                Usuario = usuario.UserName ?? "",
                Email = usuario.Email,
                Roles = await _usuarios.GetRolesAsync(usuario),
                Bloqueado = await _usuarios.IsLockedOutAsync(usuario)
            });
        }
        return View(filas);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        CargarRoles();
        return View(new CrearUsuarioViewModel { Rol = Roles.Cajero });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearUsuarioViewModel modelo)
    {
        if (!Roles.Todos.Contains(modelo.Rol))
            ModelState.AddModelError(nameof(modelo.Rol), "Rol no válido.");

        if (ModelState.IsValid)
        {
            var usuario = new IdentityUser(modelo.Usuario.Trim())
            {
                Email = modelo.Email.Trim(),
                EmailConfirmed = true
            };
            var resultado = await _usuarios.CreateAsync(usuario, modelo.Clave);
            if (resultado.Succeeded)
            {
                await _usuarios.AddToRoleAsync(usuario, modelo.Rol);
                TempData["Exito"] = $"Usuario «{usuario.UserName}» creado con rol {modelo.Rol}.";
                return RedirectToAction(nameof(Index));
            }

            // Identity devuelve los errores (contraseña débil, usuario repetido…).
            foreach (var error in resultado.Errors)
                ModelState.AddModelError(string.Empty, TraducirError(error));
        }

        CargarRoles();
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string id)
    {
        var usuario = await _usuarios.FindByIdAsync(id);
        if (usuario is null)
            return NotFound();

        if (usuario.UserName == User.Identity?.Name)
        {
            TempData["Error"] = "No puede eliminar su propia cuenta mientras está conectado.";
            return RedirectToAction(nameof(Index));
        }

        await _usuarios.DeleteAsync(usuario);
        TempData["Exito"] = $"Usuario «{usuario.UserName}» eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private void CargarRoles() => ViewBag.Roles = new SelectList(Roles.Todos);

    /// <summary>Los mensajes de Identity vienen en inglés; se traducen los más comunes por su código.</summary>
    private static string TraducirError(IdentityError error) => error.Code switch
    {
        "DuplicateUserName" => "Ese nombre de usuario ya existe.",
        "DuplicateEmail" => "Ese correo ya está registrado.",
        "PasswordRequiresDigit" => "La contraseña debe tener al menos un número.",
        "PasswordRequiresLower" => "La contraseña debe tener al menos una minúscula.",
        "PasswordRequiresUpper" => "La contraseña debe tener al menos una mayúscula.",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe tener al menos un símbolo (por ejemplo * o #).",
        "PasswordTooShort" => "La contraseña es demasiado corta.",
        "InvalidUserName" => "El nombre de usuario tiene caracteres no permitidos.",
        _ => error.Description
    };
}
