using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ambrosia.Nucleo.Datos;

/// <summary>
/// Ubicación ÚNICA de la base de datos SQLite para que la web y el escritorio usen el mismo
/// archivo: %LOCALAPPDATA%\AmbrosiaFastFood\ambrosia.db en Windows
/// (~/.local/share/AmbrosiaFastFood/ambrosia.db en Linux).
/// </summary>
public static class ConfiguracionBaseDatos
{
    public const string NombreArchivo = "ambrosia.db";

    public static string Carpeta
    {
        get
        {
            var carpeta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AmbrosiaFastFood");
            Directory.CreateDirectory(carpeta);
            return carpeta;
        }
    }

    public static string RutaPorDefecto => Path.Combine(Carpeta, NombreArchivo);

    public static string CadenaPorDefecto => $"Data Source={RutaPorDefecto}";

    /// <summary>Usa la cadena recibida o, si viene vacía, la ruta compartida por defecto.</summary>
    public static string Resolver(string? cadena) =>
        string.IsNullOrWhiteSpace(cadena) ? CadenaPorDefecto : cadena;

    public static DbContextOptions<AmbrosiaDbContext> CrearOpciones(string? cadena = null) =>
        new DbContextOptionsBuilder<AmbrosiaDbContext>()
            .UseSqlite(Resolver(cadena))
            .Options;

    /// <summary>Crea un contexto nuevo (en escritorio se crea uno por operación y se libera con using).</summary>
    public static AmbrosiaDbContext CrearContexto(string? cadena = null) => new(CrearOpciones(cadena));
}

/// <summary>
/// Fábrica que usa la herramienta "dotnet ef" en tiempo de diseño (por ejemplo, para
/// crear migraciones) sin tener que arrancar la aplicación web.
/// </summary>
public class FabricaContextoDisenio : IDesignTimeDbContextFactory<AmbrosiaDbContext>
{
    public AmbrosiaDbContext CreateDbContext(string[] args) => ConfiguracionBaseDatos.CrearContexto();
}
