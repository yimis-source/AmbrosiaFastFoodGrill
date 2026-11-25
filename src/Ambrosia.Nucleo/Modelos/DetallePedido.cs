// Entidad DetallePedido: línea de un pedido con el nombre y precio del momento de la venta.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ambrosia.Nucleo.Modelos;

public class DetallePedido : EntidadBase
{
    public int PedidoId { get; set; }
    public Pedido? Pedido { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    [StringLength(80)]
    public string NombreProducto { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    [StringLength(120)]
    public string? Nota { get; set; }

    [NotMapped]
    public decimal Subtotal => Cantidad * PrecioUnitario;
}
