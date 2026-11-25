// Login del escritorio verificando contraseñas con el PasswordHasher de Identity.
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Nucleo.Servicios;

public class ServicioAutenticacion
{
    private readonly AmbrosiaDbContext _db;
    private readonly IPasswordHasher<IdentityUser> _hasher;

    public ServicioAutenticacion(AmbrosiaDbContext db, IPasswordHasher<IdentityUser>? hasher = null)
    {
        _db = db;
        _hasher = hasher ?? new PasswordHasher<IdentityUser>();
    }

    public async Task<SesionUsuario?> IniciarSesionAsync(string usuario, string clave)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(clave))
            return null;

        var normalizado = usuario.Trim().ToUpperInvariant();
        var cuenta = await _db.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizado);
        if (cuenta?.PasswordHash is null)
            return null;

        if (cuenta.LockoutEnd is DateTimeOffset bloqueo && bloqueo > DateTimeOffset.UtcNow)
            return null;

        var verificacion = _hasher.VerifyHashedPassword(cuenta, cuenta.PasswordHash, clave);
        if (verificacion == PasswordVerificationResult.Failed)
            return null;

        var roles = await ObtenerRolesAsync(cuenta.Id);
        return new SesionUsuario(cuenta.Id, cuenta.UserName ?? usuario, roles);
    }

    public async Task<HashSet<string>> ObtenerRolesAsync(string usuarioId)
    {
        var nombres = await (from ur in _db.UserRoles
                             join r in _db.Roles on ur.RoleId equals r.Id
                             where ur.UserId == usuarioId
                             select r.Name)
                            .ToListAsync();

        return new HashSet<string>(nombres.OfType<string>(), StringComparer.OrdinalIgnoreCase);
    }
}
