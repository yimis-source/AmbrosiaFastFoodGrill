// Base SQLite temporal con la carta e insumos iniciales para las pruebas.
using Ambrosia.Nucleo.Datos;

namespace Ambrosia.Pruebas;

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

    public AmbrosiaDbContext Contexto() => ConfiguracionBaseDatos.CrearContexto(Cadena);

    public void Dispose()
    {
        try { File.Delete(_ruta); } catch (IOException) { }
    }
}
