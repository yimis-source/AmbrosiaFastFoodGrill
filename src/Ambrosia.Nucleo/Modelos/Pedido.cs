using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ambrosia.Nucleo.Modelos;

/// <summary>Orden de un cliente: para mesa, para llevar o a domicilio.</summary>
public class Pedido : EntidadBase
{
    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public TipoPedido Tipo { get; set; }

    /// <summary>Solo aplica cuando el pedido es para mesa.</summary>
    public int? Mesa { get; set; }

    [StringLength(80)]
    public string? NombreCliente { get; set; }

    [StringLength(150)]
    public string? Direccion { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

    public MetodoPago MetodoPago { get; set; }

    /// <summary>Suma de los productos.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>Cargo según el tipo: servicio en mesa o domicilio.</summary>
    public decimal Cargo { get; set; }

    public decimal Total { get; set; }

    [StringLength(250)]
    public string? Observaciones { get; set; }

    /// <summary>Usuario (cajero o administrador) que registró el pedido.</summary>
    [StringLength(60)]
    public string CreadoPor { get; set; } = string.Empty;

    public DateTime? FechaInicioPreparacion { get; set; }
    public DateTime? FechaListo { get; set; }
    public DateTime? FechaEntrega { get; set; }

    /// <summary>Líneas del pedido (relación uno a muchos, en el orden en que se digitaron).</summary>
    public List<DetallePedido> Detalles { get; set; } = new();

    // ---------- Propiedades calculadas (no se guardan en la base de datos) ----------

    /// <summary>Código corto que se canta en cocina: A-0007.</summary>
    [NotMapped]
    public string Codigo => $"A-{Id:D4}";

    [NotMapped]
    public int CantidadUnidades => Detalles.Sum(d => d.Cantidad);

    /// <summary>Texto con el destino del pedido según su tipo (switch de expresiones).</summary>
    [NotMapped]
    public string Destino => Tipo switch
    {
        TipoPedido.Mesa => $"Mesa {Mesa}",
        TipoPedido.ParaLlevar => $"Para llevar · {NombreCliente}",
        TipoPedido.Domicilio => $"Domicilio · {NombreCliente} · {Direccion}",
        _ => "Sin destino"
    };

    /// <summary>Minutos que lleva el pedido desde que se registró.</summary>
    public int MinutosDesdeCreacion(DateTime ahora) => Math.Max(0, (int)(ahora - FechaCreacion).TotalMinutes);

    /// <summary>
    /// Estimado de cocina: los productos de un pedido se preparan en paralelo,
    /// así que manda el que más se demora (requiere Detalles.Producto cargado).
    /// </summary>
    [NotMapped]
    public int MinutosEstimados => Detalles.Count == 0
        ? 0
        : Detalles.Max(d => d.Producto?.TiempoPreparacionMinutos ?? 10);
}
