// Nombres de los roles de Identity: Administrador, Cajero y Cocina.
namespace Ambrosia.Nucleo.Seguridad;

public static class Roles
{
    public const string Administrador = "Administrador";

    public const string Cajero = "Cajero";

    public const string Cocina = "Cocina";

    public const string AdministradorOCajero = Administrador + "," + Cajero;
    public const string AdministradorOCocina = Administrador + "," + Cocina;

    public static readonly string[] Todos = { Administrador, Cajero, Cocina };
}
