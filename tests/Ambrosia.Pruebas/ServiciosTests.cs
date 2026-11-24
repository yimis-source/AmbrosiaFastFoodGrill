using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Nucleo.Servicios;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Pruebas;

/// <summary>Pruebas de integración: servicios + EF Core + SQLite real (archivo temporal).</summary>
public class ServiciosTests
{
    private static async Task<int> IdProductoAsync(BaseDatosDePrueba bd, string nombre)
    {
        await using var db = bd.Contexto();
        return (await db.Productos.FirstAsync(p => p.Nombre == nombre)).Id;
    }

    private static async Task<decimal> StockAsync(BaseDatosDePrueba bd, string insumo)
    {
        await using var db = bd.Contexto();
        return (await db.Insumos.FirstAsync(i => i.Nombre == insumo)).StockActual;
    }

    private static async Task<Pedido> CrearAsync(BaseDatosDePrueba bd, SolicitudPedido solicitud, List<string>? avisos = null)
    {
        await using var db = bd.Contexto();
        var inventario = new ServicioInventario(db);
        if (avisos is not null)
            inventario.StockBajo += (_, e) => avisos.Add(e.Insumo.Nombre);
        return await new ServicioPedidos(db, inventario).CrearPedidoAsync(solicitud, "prueba");
    }

    private static SolicitudPedido ParaLlevar(params ItemSolicitado[] items) => new()
    {
        Tipo = TipoPedido.ParaLlevar,
        NombreCliente = "Cliente de prueba",
        MetodoPago = MetodoPago.Efectivo,
        Items = items.ToList()
    };

    [Fact]
    public async Task CrearPedido_DescuentaInventarioSegunLaReceta()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        var clasica = await IdProductoAsync(bd, "Ambrosía Clásica");
        var carneAntes = await StockAsync(bd, "Carne de ampolleta molida");
        var panAntes = await StockAsync(bd, "Pan brioche");

        var pedido = await CrearAsync(bd, ParaLlevar(new ItemSolicitado(clasica, 2)));

        Assert.Equal(44_000m, pedido.Total);
        Assert.Equal(EstadoPedido.Pendiente, pedido.Estado);
        Assert.Equal(carneAntes - 300, await StockAsync(bd, "Carne de ampolleta molida"));
        Assert.Equal(panAntes - 2, await StockAsync(bd, "Pan brioche"));
    }

    [Fact]
    public async Task CrearPedido_ProductoRepetido_SeConsolidaEnUnaLinea()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        var gaseosa = await IdProductoAsync(bd, "Gaseosa 400 ml");

        var pedido = await CrearAsync(bd, ParaLlevar(new ItemSolicitado(gaseosa, 1), new ItemSolicitado(gaseosa, 2)));

        Assert.Single(pedido.Detalles);
        Assert.Equal(3, pedido.Detalles[0].Cantidad);
    }

    [Fact]
    public async Task CrearPedido_SinInventario_LanzaYNoDescuentaNada()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        var costillas = await IdProductoAsync(bd, "Costillas BBQ");     // 450 g c/u; hay 1.800 g
        var papaAntes = await StockAsync(bd, "Papa a la francesa precortada");

        var error = await Assert.ThrowsAsync<StockInsuficienteException>(() =>
            CrearAsync(bd, ParaLlevar(new ItemSolicitado(costillas, 5))));

        Assert.Contains("Costilla de cerdo", error.Message);
        Assert.Equal(papaAntes, await StockAsync(bd, "Papa a la francesa precortada"));
    }

    [Fact]
    public async Task CrearPedido_MesaSinNumero_EsRechazado()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        var gaseosa = await IdProductoAsync(bd, "Gaseosa 400 ml");
        var solicitud = new SolicitudPedido { Tipo = TipoPedido.Mesa, Items = { new ItemSolicitado(gaseosa, 1) } };

        var error = await Assert.ThrowsAsync<ReglaNegocioException>(() => CrearAsync(bd, solicitud));
        Assert.Contains("mesa", error.Message);
    }

    [Fact]
    public async Task CrearPedido_CuandoUnInsumoCruzaElMinimo_DisparaElEventoStockBajo()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        var doble = await IdProductoAsync(bd, "Ambrosía Doble Tocineta");   // 40 g de tocineta; hay 1.000 g, mínimo 800 g
        var avisos = new List<string>();

        await CrearAsync(bd, ParaLlevar(new ItemSolicitado(doble, 6)), avisos);   // quedan 760 g

        Assert.Contains("Tocineta", avisos);
    }

    [Fact]
    public async Task CancelarPedidoPendiente_DevuelveElInventario()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        var churrasco = await IdProductoAsync(bd, "Churrasco 300 g");
        var antes = await StockAsync(bd, "Churrasco de res");
        var pedido = await CrearAsync(bd, ParaLlevar(new ItemSolicitado(churrasco, 2)));

        await using (var db = bd.Contexto())
            await new ServicioPedidos(db, new ServicioInventario(db)).CancelarAsync(pedido.Id);

        Assert.Equal(antes, await StockAsync(bd, "Churrasco de res"));
    }

    [Fact]
    public async Task EntregarPedidoQueNoEstaListo_LanzaTransicionInvalida()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        var gaseosa = await IdProductoAsync(bd, "Gaseosa 400 ml");
        var pedido = await CrearAsync(bd, ParaLlevar(new ItemSolicitado(gaseosa, 1)));

        await using var db = bd.Contexto();
        await Assert.ThrowsAsync<TransicionInvalidaException>(() =>
            new ServicioPedidos(db, new ServicioInventario(db)).EntregarAsync(pedido.Id));
    }

    [Fact]
    public async Task Cocina_TomarSiguiente_SacaElPedidoMasAntiguo()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        var gaseosa = await IdProductoAsync(bd, "Gaseosa 400 ml");
        var primero = await CrearAsync(bd, ParaLlevar(new ItemSolicitado(gaseosa, 1)));
        await CrearAsync(bd, ParaLlevar(new ItemSolicitado(gaseosa, 1)));

        await using var db = bd.Contexto();
        var tomado = await new ServicioPedidos(db, new ServicioInventario(db)).TomarSiguienteAsync();

        Assert.Equal(primero.Id, tomado!.Id);
        Assert.Equal(EstadoPedido.EnPreparacion, tomado.Estado);
    }

    [Fact]
    public async Task Alertas_SalenDelMasUrgenteAlMenosUrgente()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        await using var db = bd.Contexto();

        var alertas = await new ServicioInventario(db).ObtenerAlertasAsync();

        // Cerveza: 18/24 = 75 %   ·   Costilla: 1.800/2.000 = 90 %
        Assert.Equal(new[] { "Cerveza nacional", "Costilla de cerdo" }, alertas.Select(a => a.Nombre).ToArray());
    }

    [Fact]
    public async Task ProductosAgotados_DetectaRecetasSinInventario()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        await using (var db = bd.Contexto())
        {
            var cerveza = await db.Insumos.FirstAsync(i => i.Nombre == "Cerveza nacional");
            cerveza.StockActual = 0;
            await db.SaveChangesAsync();
        }

        await using var db2 = bd.Contexto();
        var agotados = await new ServicioInventario(db2).ObtenerProductosAgotadosAsync();

        Assert.Contains(await IdProductoAsync(bd, "Cerveza nacional"), agotados);
        Assert.DoesNotContain(await IdProductoAsync(bd, "Gaseosa 400 ml"), agotados);
    }

    [Fact]
    public async Task Autenticacion_UsaLosUsuariosYRolesDeIdentity()
    {
        using var bd = await BaseDatosDePrueba.CrearAsync();
        await using var db = bd.Contexto();
        var servicio = new ServicioAutenticacion(db);

        var sesion = await servicio.IniciarSesionAsync("cocina", "Cocina123*");
        var fallida = await servicio.IniciarSesionAsync("cocina", "clave-equivocada");

        Assert.NotNull(sesion);
        Assert.True(sesion!.TieneRol(Roles.Cocina));
        Assert.False(sesion.TieneRol(Roles.Administrador));
        Assert.Null(fallida);
    }
}
