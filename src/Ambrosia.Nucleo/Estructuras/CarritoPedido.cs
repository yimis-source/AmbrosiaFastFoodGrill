using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Estructuras;

/// <summary>Línea del carrito. Es un record: para "cambiarla" se crea una copia con "with".</summary>
public record LineaCarrito(int ProductoId, string Nombre, decimal PrecioUnitario, int Cantidad, string? Nota = null)
{
    public decimal Subtotal => PrecioUnitario * Cantidad;
}

/// <summary>
/// Pedido en construcción en la caja, antes de registrarlo.
///
/// ESTRUCTURAS USADAS
/// • Dictionary&lt;int, LineaCarrito&gt;: la clave es el Id del producto. Si el cajero marca
///   dos veces "Ambrosía Clásica", se encuentra la línea existente en O(1) y se suma la
///   cantidad, en lugar de recorrer una lista O(n) o crear líneas duplicadas.
/// • Stack&lt;...&gt; (pila LIFO): historial para el botón "Deshacer". La última acción es la
///   primera que se revierte, que es exactamente el comportamiento de una pila.
/// </summary>
public class CarritoPedido
{
    private readonly Dictionary<int, LineaCarrito> _lineas = new();

    // Cada acción guarda cómo estaba la línea ANTES de modificarse (null = no existía).
    private readonly Stack<(int ProductoId, LineaCarrito? Anterior)> _historial = new();

    /// <summary>EVENTO: avisa a la interfaz que debe refrescar la vista del carrito.</summary>
    public event EventHandler? Cambiado;

    public IReadOnlyCollection<LineaCarrito> Lineas => _lineas.Values;
    public int CantidadLineas => _lineas.Count;
    public int CantidadUnidades => _lineas.Values.Sum(l => l.Cantidad);
    public decimal Subtotal => _lineas.Values.Sum(l => l.Subtotal);
    public bool EstaVacio => _lineas.Count == 0;
    public bool PuedeDeshacer => _historial.Count > 0;
    public int AccionesEnHistorial => _historial.Count;

    /// <summary>INDEXADOR: carrito[idProducto] devuelve la línea o null.</summary>
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

    /// <summary>Quita una unidad del producto (si queda en cero, quita la línea).</summary>
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

    /// <summary>Quita la línea completa del producto.</summary>
    public bool Quitar(int productoId)
    {
        if (!_lineas.Remove(productoId, out var anterior)) return false;
        _historial.Push((productoId, anterior));
        AlCambiar();
        return true;
    }

    /// <summary>Revierte la última acción (Pop de la pila).</summary>
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

    /// <summary>Deja el carrito vacío (después de registrar el pedido). No se puede deshacer.</summary>
    public void Vaciar()
    {
        _lineas.Clear();
        _historial.Clear();
        AlCambiar();
    }

    /// <summary>Convierte el carrito en los ítems que recibe el servicio de pedidos.</summary>
    public List<ItemSolicitado> ComoItems() =>
        _lineas.Values.Select(l => new ItemSolicitado(l.ProductoId, l.Cantidad, l.Nota)).ToList();

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private void AlCambiar() => Cambiado?.Invoke(this, EventArgs.Empty);
}
