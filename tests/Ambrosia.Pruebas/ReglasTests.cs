// Pruebas de reglas puras: cargos, transiciones de estado, formatos y agrupaciones.
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Nucleo.Utilidades;

namespace Ambrosia.Pruebas;

public class ReglasTests
{
    [Theory]
    [InlineData(TipoPedido.Mesa, 50_000, 5_000)]
    [InlineData(TipoPedido.ParaLlevar, 50_000, 0)]
    [InlineData(TipoPedido.Domicilio, 50_000, 4_000)]
    [InlineData(TipoPedido.Domicilio, 80_000, 0)]
    public void Cargo_SegunTipoDePedido(TipoPedido tipo, int subtotal, int cargoEsperado)
    {
        ICalculadorCargo calculador = FabricaCargos.Para(tipo);
        Assert.Equal((decimal)cargoEsperado, calculador.Calcular(subtotal));
    }

    [Fact]
    public void Polimorfismo_CadaCargoRedefineSuComportamiento()
    {
        var cargos = new List<CalculadorCargo> { new SinCargo(), new CargoServicioMesa(), new CargoDomicilio() };

        var valores = new List<decimal>();
        foreach (var cargo in cargos)
            valores.Add(cargo.Calcular(50_000m));

        Assert.Equal(new List<decimal> { 0m, 5_000m, 4_000m }, valores);
        Assert.Equal("Sin cargo adicional", cargos[0].Descripcion);
        Assert.Equal("Servicio voluntario (10 %)", cargos[1].Descripcion);
    }

    [Theory]
    [InlineData(EstadoPedido.Pendiente, EstadoPedido.EnPreparacion, true)]
    [InlineData(EstadoPedido.Pendiente, EstadoPedido.Cancelado, true)]
    [InlineData(EstadoPedido.EnPreparacion, EstadoPedido.Listo, true)]
    [InlineData(EstadoPedido.Listo, EstadoPedido.Entregado, true)]
    [InlineData(EstadoPedido.Pendiente, EstadoPedido.Entregado, false)]
    [InlineData(EstadoPedido.EnPreparacion, EstadoPedido.Cancelado, false)]
    [InlineData(EstadoPedido.Entregado, EstadoPedido.Pendiente, false)]
    public void Transiciones_DeEstado(EstadoPedido actual, EstadoPedido nuevo, bool valida)
    {
        Assert.Equal(valida, ServicioPedidos.EsTransicionValida(actual, nuevo));
    }

    [Fact]
    public void Formatos_PesosYCantidadesEnEstiloColombiano()
    {
        Assert.Equal("$ 18.000", 18_000m.ComoPesos());
        Assert.Equal("$ 1.250.500", 1_250_500m.ComoPesos());
        Assert.Equal("1.500,5 g", 1_500.5m.ComoCantidad(UnidadMedida.Gramo));
        Assert.Equal("En preparación", EstadoPedido.EnPreparacion.Texto());
    }

    [Fact]
    public void AgruparTotales_EsGenericoParaCualquierClave()
    {
        var pedidos = new List<Pedido>
        {
            new() { MetodoPago = MetodoPago.Nequi, Tipo = TipoPedido.Mesa, Total = 10_000 },
            new() { MetodoPago = MetodoPago.Nequi, Tipo = TipoPedido.Domicilio, Total = 5_000 },
            new() { MetodoPago = MetodoPago.Efectivo, Tipo = TipoPedido.Mesa, Total = 2_000 },
        };

        var porPago = ServicioReportes.AgruparTotales(pedidos, p => p.MetodoPago);
        var porTipo = ServicioReportes.AgruparTotales(pedidos, p => p.Tipo);

        Assert.Equal(15_000m, porPago[MetodoPago.Nequi]);
        Assert.Equal(12_000m, porTipo[TipoPedido.Mesa]);
    }

    [Fact]
    public void VentasPorDia_QuedanOrdenadasEIncluyenDiasSinVentas()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var rango = new RangoFechas(hoy.AddDays(-2), hoy);
        var pedidos = new List<Pedido>
        {
            new() { FechaCreacion = DateTime.Now, Total = 30_000 },
            new() { FechaCreacion = DateTime.Now.AddDays(-2), Total = 10_000 },
        };

        var porDia = ServicioReportes.VentasPorDia(pedidos, rango);

        Assert.Equal(new[] { hoy.AddDays(-2), hoy.AddDays(-1), hoy }, porDia.Keys.ToArray());
        Assert.Equal(0m, porDia[hoy.AddDays(-1)].Total);
        Assert.Equal(30_000m, porDia[hoy].Total);
    }

    [Fact]
    public void Requerimientos_SumanElMismoInsumoDeVariosProductos()
    {
        var clasica = new Producto { Id = 1, Receta = { new RecetaItem { InsumoId = 10, Cantidad = 1 }, new RecetaItem { InsumoId = 20, Cantidad = 150 } } };
        var doble = new Producto { Id = 2, Receta = { new RecetaItem { InsumoId = 10, Cantidad = 1 }, new RecetaItem { InsumoId = 20, Cantidad = 300 } } };

        var requerimientos = ServicioInventario.CalcularRequerimientos(new[] { (clasica, 2), (doble, 1) });

        Assert.Equal(3m, requerimientos[10]);
        Assert.Equal(600m, requerimientos[20]);
    }
}
