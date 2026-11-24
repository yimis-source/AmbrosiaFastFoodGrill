using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ambrosia.Nucleo.Modelos;

/// <summary>Línea de un pedido: producto, cantidad y precio en el momento de la venta.</summary>
public class DetallePedido : EntidadBase
{
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    /// <summary>
    /// Copia del nombre y del precio al momento de vender: si mañana cambia el precio
    /// en la carta, los pedidos y reportes anteriores no se alteran.
    /// </summary>
    [StringLength(80)]
    public string NombreProducto { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    /// <summary>Indicación para cocina: "sin cebolla", "término medio"…</summary>
    [StringLength(120)]
    public string? Nota { get; set; }

    [NotMapped]
    public decimal Subtotal => Cantidad * PrecioUnitario;
}
