using Ambrosia.Nucleo.Excepciones;

namespace Ambrosia.Nucleo.Modelos;

// RECORDS: tipos inmutables pensados para transportar datos (DTO).
// Comparan por valor, traen ToString() legible y permiten copias con "with".

/// <summary>Un producto que el cajero quiere agregar a un pedido.</summary>
public record ItemSolicitado(int ProductoId, int Cantidad, string? Nota = null);

/// <summary>Datos que llegan desde la caja (web o escritorio) para crear un pedido.</summary>
public record SolicitudPedido
{
    public TipoPedido Tipo { get; init; }
    public MetodoPago MetodoPago { get; init; }
    public int? Mesa { get; init; }
    public string? NombreCliente { get; init; }
    public string? Direccion { get; init; }
    public string? Telefono { get; init; }
    public string? Observaciones { get; init; }
    public List<ItemSolicitado> Items { get; init; } = new();
}

/// <summary>Una sección de la carta con sus productos.</summary>
public record GrupoMenu(Categoria Categoria, List<Producto> Productos);

/// <summary>Ventas de un día (fila del reporte).</summary>
public record VentaDiaria(DateOnly Dia, int Pedidos, decimal Total);

/// <summary>Producto del ranking de más vendidos.</summary>
public record ProductoVendido(string Nombre, int Unidades, decimal Ingresos);

/// <summary>Usuario autenticado en la aplicación de escritorio.</summary>
public record SesionUsuario(string Id, string NombreUsuario, IReadOnlySet<string> Roles)
{
    public bool TieneRol(string rol) => Roles.Contains(rol);
}

/// <summary>
/// RECORD STRUCT de solo lectura: un rango de fechas es un valor pequeño (dos fechas),
/// por eso se modela como struct (vive en la pila, sin costo de recolección de basura).
/// El constructor valida la regla "hasta no puede ser anterior a desde".
/// </summary>
public readonly record struct RangoFechas
{
    public DateOnly Desde { get; }
    public DateOnly Hasta { get; }

    public RangoFechas(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
            throw new ReglaNegocioException("La fecha final no puede ser anterior a la fecha inicial.");
        Desde = desde;
        Hasta = hasta;
    }

    /// <summary>Primer instante del rango (00:00 del día inicial).</summary>
    public DateTime Inicio => Desde.ToDateTime(TimeOnly.MinValue);

    /// <summary>Primer instante DESPUÉS del rango (00:00 del día siguiente al final).</summary>
    public DateTime FinExclusivo => Hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);

    public int TotalDias => Hasta.DayNumber - Desde.DayNumber + 1;

    public static RangoFechas UltimosDias(int dias)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        return new RangoFechas(hoy.AddDays(-(dias - 1)), hoy);
    }
}

/// <summary>Resultado del reporte de ventas de un periodo.</summary>
public class ReporteVentas
{
    public RangoFechas Rango { get; init; }
    public int NumeroPedidos { get; init; }
    public int PedidosCancelados { get; init; }
    public decimal TotalVentas { get; init; }
    public decimal TicketPromedio => NumeroPedidos == 0 ? 0 : Math.Round(TotalVentas / NumeroPedidos, 0);

    /// <summary>SortedDictionary: los días quedan ordenados por fecha automáticamente.</summary>
    public SortedDictionary<DateOnly, VentaDiaria> VentasPorDia { get; init; } = new();

    public Dictionary<MetodoPago, decimal> PorMetodoPago { get; init; } = new();
    public Dictionary<TipoPedido, decimal> PorTipoPedido { get; init; } = new();
    public Dictionary<string, decimal> PorUsuario { get; init; } = new();
    public List<ProductoVendido> TopProductos { get; init; } = new();
}
