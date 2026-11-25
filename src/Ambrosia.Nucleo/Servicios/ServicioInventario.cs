// Inventario: consumo según recetas, existencias, descuentos, compras y alertas (evento StockBajo).
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Utilidades;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Nucleo.Servicios;

public class StockBajoEventArgs : EventArgs
{
    public Insumo Insumo { get; }
    public StockBajoEventArgs(Insumo insumo) => Insumo = insumo;
    public string Mensaje =>
        $"«{Insumo.Nombre}» quedó en {Insumo.StockActual.ComoCantidad(Insumo.Unidad)} " +
        $"(mínimo {Insumo.StockMinimo.ComoCantidad(Insumo.Unidad)}). Hay que comprar.";
}

public class ServicioInventario
{
    private readonly AmbrosiaDbContext _db;

    public event EventHandler<StockBajoEventArgs>? StockBajo;

    public ServicioInventario(AmbrosiaDbContext db) => _db = db;

    public Task<List<Insumo>> ListarAsync() =>
        _db.Insumos.OrderBy(i => i.Nombre).ToListAsync();

    public async Task<Insumo> ObtenerAsync(int id) =>
        await _db.Insumos.FirstOrDefaultAsync(i => i.Id == id)
        ?? throw new EntidadNoEncontradaException("insumo", id);

    public async Task<HashSet<int>> ObtenerProductosAgotadosAsync()
    {
        var recetas = await _db.RecetaItems.Include(r => r.Insumo).ToListAsync();
        return recetas
            .Where(r => r.Insumo is not null && r.Insumo.StockActual < r.Cantidad)
            .Select(r => r.ProductoId)
            .ToHashSet();
    }

    public async Task<List<Insumo>> ObtenerAlertasAsync(int maximo = int.MaxValue)
    {
        var insumos = await _db.Insumos.ToListAsync();

        var cola = new PriorityQueue<Insumo, decimal>(
            insumos.Where(i => i.BajoMinimo).Select(i => (i, i.NivelStock)));

        var resultado = new List<Insumo>();
        while (resultado.Count < maximo && cola.TryDequeue(out var urgente, out _))
            resultado.Add(urgente);
        return resultado;
    }

    public async Task<decimal> ValorInventarioAsync()
    {
        var insumos = await _db.Insumos.ToListAsync();
        return insumos.Sum(i => i.ValorEnBodega);
    }

    public async Task CrearAsync(Insumo insumo)
    {
        await ValidarNombreUnicoAsync(insumo.Nombre, idActual: 0);
        insumo.Nombre = insumo.Nombre.Trim();
        _db.Insumos.Add(insumo);
        await _db.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Insumo datos)
    {
        var insumo = await ObtenerAsync(datos.Id);
        await ValidarNombreUnicoAsync(datos.Nombre, datos.Id);
        insumo.Nombre = datos.Nombre.Trim();
        insumo.Unidad = datos.Unidad;
        insumo.StockActual = datos.StockActual;
        insumo.StockMinimo = datos.StockMinimo;
        insumo.CostoUnitario = datos.CostoUnitario;
        await _db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        var insumo = await ObtenerAsync(id);
        if (await _db.RecetaItems.AnyAsync(r => r.InsumoId == id))
            throw new ReglaNegocioException($"No se puede eliminar «{insumo.Nombre}»: hace parte de una o más recetas.");
        _db.Insumos.Remove(insumo);
        await _db.SaveChangesAsync();
    }

    public async Task<Insumo> ReabastecerAsync(int id, decimal cantidad)
    {
        if (cantidad <= 0)
            throw new ReglaNegocioException("La cantidad a ingresar debe ser mayor que cero.");
        var insumo = await ObtenerAsync(id);
        insumo.StockActual += cantidad;
        await _db.SaveChangesAsync();
        return insumo;
    }

    public static Dictionary<int, decimal> CalcularRequerimientos(IEnumerable<(Producto Producto, int Cantidad)> lineas)
    {
        var requerimientos = new Dictionary<int, decimal>();
        foreach (var (producto, cantidad) in lineas)
        {
            foreach (var item in producto.Receta)
            {
                requerimientos[item.InsumoId] =
                    requerimientos.GetValueOrDefault(item.InsumoId) + item.Cantidad * cantidad;
            }
        }
        return requerimientos;
    }

    public static List<string> BuscarFaltantes(
        IReadOnlyDictionary<int, decimal> requerimientos,
        IReadOnlyDictionary<int, Insumo> insumos)
    {
        var faltantes = new List<string>();
        foreach (var (insumoId, necesario) in requerimientos)
        {
            if (!insumos.TryGetValue(insumoId, out var insumo)) continue;
            if (insumo.StockActual < necesario)
            {
                faltantes.Add($"{insumo.Nombre}: hay {insumo.StockActual.ComoCantidad(insumo.Unidad)} " +
                              $"y se necesitan {necesario.ComoCantidad(insumo.Unidad)}");
            }
        }
        return faltantes;
    }

    internal void Descontar(IReadOnlyDictionary<int, decimal> requerimientos, IReadOnlyDictionary<int, Insumo> insumos)
    {
        foreach (var (insumoId, cantidad) in requerimientos)
        {
            var insumo = insumos[insumoId];
            var estabaBien = !insumo.BajoMinimo;
            insumo.StockActual -= cantidad;
            if (estabaBien && insumo.BajoMinimo)
                AlBajarStock(insumo);
        }
    }

    internal static void Devolver(IReadOnlyDictionary<int, decimal> requerimientos, IEnumerable<Insumo> insumos)
    {
        foreach (var insumo in insumos)
        {
            if (requerimientos.TryGetValue(insumo.Id, out var cantidad))
                insumo.StockActual += cantidad;
        }
    }

    protected virtual void AlBajarStock(Insumo insumo) =>
        StockBajo?.Invoke(this, new StockBajoEventArgs(insumo));

    private async Task ValidarNombreUnicoAsync(string nombre, int idActual)
    {
        var limpio = nombre.Trim();
        if (await _db.Insumos.AnyAsync(i => i.Nombre == limpio && i.Id != idActual))
            throw new ReglaNegocioException($"Ya existe un insumo llamado «{limpio}».");
    }
}
