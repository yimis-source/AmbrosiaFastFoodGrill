using Ambrosia.Nucleo.Datos;

namespace Ambrosia.Pruebas;

/// <summary>
/// Crea una base de datos SQLite temporal con la carta e insumos iniciales (sin pedidos de ejemplo)
/// y la borra al terminar la prueba. Cada prueba trabaja con datos limpios.
/// </summary>
public sealed class BaseDatosDePrueba : IDisposable
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"ambrosia-prueba-{Guid.NewGuid():N}.db");

    public string Cadena => $"Data Source={_ruta};Pooling=False";

    public static async Task<BaseDatosDePrueba> CrearAsync()
    {
        var bd = new BaseDatosDePrueba();
        await using var db = bd.Contexto();
        await InicializadorBaseDatos.InicializarAsync(db, incluirPedidosDeEjemplo: false);
        return bd;
    }

    /// <summary>Contexto nuevo (como en la app real: uno por operación).</summary>
    public AmbrosiaDbContext Contexto() => ConfiguracionBaseDatos.CrearContexto(Cadena);

    public void Dispose()
    {
        try { File.Delete(_ruta); } catch (IOException) { /* el archivo temporal se limpia luego */ }
    }
}
