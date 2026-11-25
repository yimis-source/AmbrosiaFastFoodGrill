// Entidad Pedido: tipo, estado, pago, totales y sus líneas de detalle.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ambrosia.Nucleo.Modelos;

public class Pedido : EntidadBase
{
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public TipoPedido Tipo { get; set; }

    public int? Mesa { get; set; }

    [StringLength(80)]
    public string? NombreCliente { get; set; }

    [StringLength(150)]
    public string? Direccion { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

    public MetodoPago MetodoPago { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Cargo { get; set; }

    public decimal Total { get; set; }

    [StringLength(250)]
    public string? Observaciones { get; set; }

    [StringLength(60)]
    public string CreadoPor { get; set; } = string.Empty;

    public DateTime? FechaInicioPreparacion { get; set; }
    public DateTime? FechaListo { get; set; }
    public DateTime? FechaEntrega { get; set; }

    public List<DetallePedido> Detalles { get; set; } = new();

    [NotMapped]
    public string Codigo => $"A-{Id:D4}";

    [NotMapped]
    public int CantidadUnidades => Detalles.Sum(d => d.Cantidad);

    [NotMapped]
    public string Destino => Tipo switch
    {
        TipoPedido.Mesa => $"Mesa {Mesa}",
        TipoPedido.ParaLlevar => $"Para llevar · {NombreCliente}",
        TipoPedido.Domicilio => $"Domicilio · {NombreCliente} · {Direccion}",
        _ => "Sin destino"
    };

    public int MinutosDesdeCreacion(DateTime ahora) => Math.Max(0, (int)(ahora - FechaCreacion).TotalMinutes);

    [NotMapped]
    public int MinutosEstimados => Detalles.Count == 0
        ? 0
        : Detalles.Max(d => d.Producto?.TiempoPreparacionMinutos ?? 10);
}
