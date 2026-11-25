// Reporte de ventas: por día, método de pago, tipo de pedido y productos más vendidos.
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Nucleo.Servicios;

public class ServicioReportes
{
    private readonly AmbrosiaDbContext _db;

    public ServicioReportes(AmbrosiaDbContext db) => _db = db;

    public async Task<ReporteVentas> GenerarAsync(RangoFechas rango)
    {
        var inicio = rango.Inicio;
        var fin = rango.FinExclusivo;

        var pedidos = await _db.Pedidos
            .Include(p => p.Detalles)
            .Where(p => p.FechaCreacion >= inicio && p.FechaCreacion < fin)
            .ToListAsync();

        var validos = pedidos.Where(p => p.Estado != EstadoPedido.Cancelado).ToList();

        return new ReporteVentas
        {
            Rango = rango,
            NumeroPedidos = validos.Count,
            PedidosCancelados = pedidos.Count - validos.Count,
            TotalVentas = validos.Sum(p => p.Total),
            VentasPorDia = VentasPorDia(validos, rango),
            PorMetodoPago = AgruparTotales(validos, p => p.MetodoPago),
            PorTipoPedido = AgruparTotales(validos, p => p.Tipo),
            PorUsuario = AgruparTotales(validos, p => p.CreadoPor),
            TopProductos = MasVendidos(validos, 5)
        };
    }

    public static SortedDictionary<DateOnly, VentaDiaria> VentasPorDia(IEnumerable<Pedido> pedidos, RangoFechas rango)
    {
        var porDia = new SortedDictionary<DateOnly, VentaDiaria>();
        for (var dia = rango.Desde; dia <= rango.Hasta; dia = dia.AddDays(1))
            porDia[dia] = new VentaDiaria(dia, 0, 0m);

        foreach (var pedido in pedidos)
        {
            var dia = DateOnly.FromDateTime(pedido.FechaCreacion);
            if (!porDia.TryGetValue(dia, out var venta)) continue;
            porDia[dia] = venta with { Pedidos = venta.Pedidos + 1, Total = venta.Total + pedido.Total };
        }
        return porDia;
    }

    public static Dictionary<TClave, decimal> AgruparTotales<TClave>(
        IEnumerable<Pedido> pedidos, Func<Pedido, TClave> selectorClave) where TClave : notnull
    {
        var resultado = new Dictionary<TClave, decimal>();
        foreach (var pedido in pedidos)
        {
            var clave = selectorClave(pedido);
            resultado[clave] = resultado.GetValueOrDefault(clave) + pedido.Total;
        }
        return resultado;
    }

    public static List<ProductoVendido> MasVendidos(IEnumerable<Pedido> pedidos, int cuantos) =>
        pedidos
            .SelectMany(p => p.Detalles)
            .GroupBy(d => d.NombreProducto)
            .Select(g => new ProductoVendido(g.Key, g.Sum(d => d.Cantidad), g.Sum(d => d.Subtotal)))
            .OrderByDescending(x => x.Unidades)
            .ThenByDescending(x => x.Ingresos)
            .Take(cuantos)
            .ToList();
}
