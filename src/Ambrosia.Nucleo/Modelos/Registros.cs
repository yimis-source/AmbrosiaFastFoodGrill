// Records para transportar datos: solicitud de pedido, rango de fechas, sesión y reporte.
using Ambrosia.Nucleo.Excepciones;

namespace Ambrosia.Nucleo.Modelos;

public record ItemSolicitado(int ProductoId, int Cantidad, string? Nota = null);

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

public record GrupoMenu(Categoria Categoria, List<Producto> Productos);

public record VentaDiaria(DateOnly Dia, int Pedidos, decimal Total);

public record ProductoVendido(string Nombre, int Unidades, decimal Ingresos);

public record SesionUsuario(string Id, string NombreUsuario, IReadOnlySet<string> Roles)
{
    public bool TieneRol(string rol) => Roles.Contains(rol);
}

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

    public DateTime Inicio => Desde.ToDateTime(TimeOnly.MinValue);

    public DateTime FinExclusivo => Hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);

    public int TotalDias => Hasta.DayNumber - Desde.DayNumber + 1;

    public static RangoFechas UltimosDias(int dias)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        return new RangoFechas(hoy.AddDays(-(dias - 1)), hoy);
    }
}

public class ReporteVentas
{
    public RangoFechas Rango { get; init; }
    public int NumeroPedidos { get; init; }
    public int PedidosCancelados { get; init; }
    public decimal TotalVentas { get; init; }
    public decimal TicketPromedio => NumeroPedidos == 0 ? 0 : Math.Round(TotalVentas / NumeroPedidos, 0);

    public SortedDictionary<DateOnly, VentaDiaria> VentasPorDia { get; init; } = new();

    public Dictionary<MetodoPago, decimal> PorMetodoPago { get; init; } = new();
    public Dictionary<TipoPedido, decimal> PorTipoPedido { get; init; } = new();
    public Dictionary<string, decimal> PorUsuario { get; init; } = new();
    public List<ProductoVendido> TopProductos { get; init; } = new();
}
