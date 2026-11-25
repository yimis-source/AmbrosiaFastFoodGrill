// Ubicación compartida del archivo SQLite para que web y escritorio usen la misma base.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ambrosia.Nucleo.Datos;

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

    public static string Resolver(string? cadena) =>
        string.IsNullOrWhiteSpace(cadena) ? CadenaPorDefecto : cadena;

    public static DbContextOptions<AmbrosiaDbContext> CrearOpciones(string? cadena = null) =>
        new DbContextOptionsBuilder<AmbrosiaDbContext>()
            .UseSqlite(Resolver(cadena))
            .Options;

    public static AmbrosiaDbContext CrearContexto(string? cadena = null) => new(CrearOpciones(cadena));
}

public class FabricaContextoDisenio : IDesignTimeDbContextFactory<AmbrosiaDbContext>
{
    public AmbrosiaDbContext CreateDbContext(string[] args) => ConfiguracionBaseDatos.CrearContexto();
}
