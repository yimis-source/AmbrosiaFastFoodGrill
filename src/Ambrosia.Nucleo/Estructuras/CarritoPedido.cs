// Pedido en construcción: Dictionary para no repetir productos y Stack para deshacer.
using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Estructuras;

public record LineaCarrito(int ProductoId, string Nombre, decimal PrecioUnitario, int Cantidad, string? Nota = null)
{
    public decimal Subtotal => PrecioUnitario * Cantidad;
}

public class CarritoPedido
{
    private readonly Dictionary<int, LineaCarrito> _lineas = new();

    private readonly Stack<(int ProductoId, LineaCarrito? Anterior)> _historial = new();

    public event EventHandler? Cambiado;

    public IReadOnlyCollection<LineaCarrito> Lineas => _lineas.Values;
    public int CantidadLineas => _lineas.Count;
    public int CantidadUnidades => _lineas.Values.Sum(l => l.Cantidad);
    public decimal Subtotal => _lineas.Values.Sum(l => l.Subtotal);
    public bool EstaVacio => _lineas.Count == 0;
    public bool PuedeDeshacer => _historial.Count > 0;
    public int AccionesEnHistorial => _historial.Count;

    public LineaCarrito? this[int productoId] =>
        _lineas.TryGetValue(productoId, out var linea) ? linea : null;

    public void Agregar(Producto producto, int cantidad = 1, string? nota = null) =>
        Agregar(producto.Id, producto.Nombre, producto.Precio, cantidad, nota);

    public void Agregar(int productoId, string nombre, decimal precioUnitario, int cantidad = 1, string? nota = null)
    {
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser mayor que cero.");

        _lineas.TryGetValue(productoId, out var actual);
        _historial.Push((productoId, actual));

        _lineas[productoId] = actual is null
            ? new LineaCarrito(productoId, nombre, precioUnitario, cantidad, Limpiar(nota))
            : actual with
            {
                Cantidad = actual.Cantidad + cantidad,
                Nota = Limpiar(nota) ?? actual.Nota
            };

        AlCambiar();
    }

    public bool QuitarUno(int productoId)
    {
        if (!_lineas.TryGetValue(productoId, out var actual)) return false;
        _historial.Push((productoId, actual));

        if (actual.Cantidad > 1)
            _lineas[productoId] = actual with { Cantidad = actual.Cantidad - 1 };
        else
            _lineas.Remove(productoId);

        AlCambiar();
        return true;
    }

    public bool Quitar(int productoId)
    {
        if (!_lineas.Remove(productoId, out var anterior)) return false;
        _historial.Push((productoId, anterior));
        AlCambiar();
        return true;
    }

    public bool Deshacer()
    {
        if (_historial.Count == 0) return false;
        var accion = _historial.Pop();

        if (accion.Anterior is null)
            _lineas.Remove(accion.ProductoId);
        else
            _lineas[accion.ProductoId] = accion.Anterior;

        AlCambiar();
        return true;
    }

    public void Vaciar()
    {
        _lineas.Clear();
        _historial.Clear();
        AlCambiar();
    }

    public List<ItemSolicitado> ComoItems() =>
        _lineas.Values.Select(l => new ItemSolicitado(l.ProductoId, l.Cantidad, l.Nota)).ToList();

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private void AlCambiar() => Cambiado?.Invoke(this, EventArgs.Empty);
}
