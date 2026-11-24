namespace Ambrosia.Nucleo.Seguridad;

/// <summary>
/// Roles de ASP.NET Core Identity. Son constantes para poder usarlas en atributos:
/// [Authorize(Roles = Roles.AdministradorOCajero)].
/// </summary>
public static class Roles
{
    /// <summary>Dueño / administrador: carta, inventario, reportes y usuarios.</summary>
    public const string Administrador = "Administrador";

    /// <summary>Caja: registra, entrega y cancela pedidos.</summary>
    public const string Cajero = "Cajero";

    /// <summary>Cocina: atiende la cola de pedidos y los marca como listos.</summary>
    public const string Cocina = "Cocina";

    public const string AdministradorOCajero = Administrador + "," + Cajero;
    public const string AdministradorOCocina = Administrador + "," + Cocina;

    public static readonly string[] Todos = { Administrador, Cajero, Cocina };
}
