using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Nucleo.Servicios;

/// <summary>
/// Inicio de sesión para la aplicación de ESCRITORIO usando las mismas tablas y el mismo
/// algoritmo de contraseñas de ASP.NET Core Identity (PasswordHasher). Así un usuario creado
/// en la web entra también al escritorio con su rol.
/// (La web no usa esta clase: usa SignInManager de Identity.)
/// </summary>
public class ServicioAutenticacion
{
    private readonly AmbrosiaDbContext _db;
    private readonly IPasswordHasher<IdentityUser> _hasher;

    public ServicioAutenticacion(AmbrosiaDbContext db, IPasswordHasher<IdentityUser>? hasher = null)
    {
        _db = db;
        _hasher = hasher ?? new PasswordHasher<IdentityUser>();
    }

    /// <summary>Devuelve la sesión si usuario y contraseña son correctos; null en caso contrario.</summary>
    public async Task<SesionUsuario?> IniciarSesionAsync(string usuario, string clave)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(clave))
            return null;

        var normalizado = usuario.Trim().ToUpperInvariant();
        var cuenta = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizado);
        if (cuenta?.PasswordHash is null)
            return null;

        // Respeta el bloqueo que aplica Identity en la web tras varios intentos fallidos.
        if (cuenta.LockoutEnd is DateTimeOffset bloqueo && bloqueo > DateTimeOffset.UtcNow)
            return null;

        var verificacion = _hasher.VerifyHashedPassword(cuenta, cuenta.PasswordHash, clave);
        if (verificacion == PasswordVerificationResult.Failed)
            return null;

        var roles = await ObtenerRolesAsync(cuenta.Id);
        return new SesionUsuario(cuenta.Id, cuenta.UserName ?? usuario, roles);
    }

    /// <summary>Roles del usuario (consulta LINQ con sintaxis de query y join).</summary>
    public async Task<HashSet<string>> ObtenerRolesAsync(string usuarioId)
    {
        var nombres = await (from ur in _db.UserRoles
                             join r in _db.Roles on ur.RoleId equals r.Id
                             where ur.UserId == usuarioId
                             select r.Name)
                            .ToListAsync();

        // HashSet: TieneRol("Cocina") es O(1) y no hay roles repetidos.
        return new HashSet<string>(nombres.OfType<string>(), StringComparer.OrdinalIgnoreCase);
    }
}
