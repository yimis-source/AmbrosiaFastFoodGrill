using Ambrosia.Nucleo.Estructuras;
using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Pruebas;

/// <summary>Pruebas de las estructuras de datos propias (sin base de datos).</summary>
public class EstructurasTests
{
    // ------------------------------------------------------------------ CarritoPedido

    [Fact]
    public void Carrito_AgregarMismoProducto_SumaEnUnaSolaLinea()
    {
        var carrito = new CarritoPedido();
        carrito.Agregar(1, "Ambrosía Clásica", 22_000m, 1);
        carrito.Agregar(1, "Ambrosía Clásica", 22_000m, 2);

        Assert.Equal(1, carrito.CantidadLineas);       // Dictionary: una sola clave
        Assert.Equal(3, carrito[1]!.Cantidad);
        Assert.Equal(66_000m, carrito.Subtotal);
    }

    [Fact]
    public void Carrito_Deshacer_RevierteEnOrdenInverso()
    {
        var carrito = new CarritoPedido();
        carrito.Agregar(1, "Ambrosía Clásica", 22_000m);
        carrito.Agregar(2, "Gaseosa 400 ml", 4_500m);
        carrito.Agregar(1, "Ambrosía Clásica", 22_000m);

        Assert.True(carrito.Deshacer());                // quita la última acción (+1 clásica)
        Assert.Equal(1, carrito[1]!.Cantidad);
        Assert.True(carrito.Deshacer());                // quita la gaseosa
        Assert.Null(carrito[2]);
        Assert.True(carrito.Deshacer());                // quita la primera clásica
        Assert.True(carrito.EstaVacio);
        Assert.False(carrito.Deshacer());               // pila vacía
    }

    [Fact]
    public void Carrito_QuitarYDeshacer_RestauraLaLinea()
    {
        var carrito = new CarritoPedido();
        carrito.Agregar(5, "Churrasco 300 g", 38_000m, 2, "término medio");
        carrito.Quitar(5);
        Assert.True(carrito.EstaVacio);

        carrito.Deshacer();
        Assert.Equal(2, carrito[5]!.Cantidad);
        Assert.Equal("término medio", carrito[5]!.Nota);
    }

    [Fact]
    public void Carrito_EventoCambiado_SeDisparaEnCadaModificacion()
    {
        var carrito = new CarritoPedido();
        var veces = 0;
        carrito.Cambiado += (_, _) => veces++;

        carrito.Agregar(1, "Perro Ambrosía", 15_000m);
        carrito.QuitarUno(1);
        carrito.Deshacer();

        Assert.Equal(3, veces);
    }

    [Fact]
    public void Carrito_CantidadCeroONegativa_Lanza()
    {
        var carrito = new CarritoPedido();
        Assert.Throws<ArgumentOutOfRangeException>(() => carrito.Agregar(1, "X", 1_000m, 0));
    }

    // ------------------------------------------------------------------ ColaCocina

    [Fact]
    public void ColaCocina_AtiendeEnOrdenDeLlegada()
    {
        var ahora = DateTime.Now;
        var pedidos = new List<Pedido>
        {
            new() { Id = 3, FechaCreacion = ahora.AddMinutes(-1) },
            new() { Id = 1, FechaCreacion = ahora.AddMinutes(-10) },
            new() { Id = 2, FechaCreacion = ahora.AddMinutes(-5) },
        };

        var cola = new ColaCocina(pedidos);

        Assert.Equal(1, cola.Siguiente!.Id);
        Assert.Equal(3, cola.PosicionDe(3));
        Assert.Equal(1, cola.TomarSiguiente()!.Id);      // Dequeue: sale el más antiguo
        Assert.Equal(2, cola.TomarSiguiente()!.Id);
        Assert.Equal(1, cola.Cantidad);
    }

    [Fact]
    public void ColaCocina_Vacia_NoEntregaPedido()
    {
        var cola = new ColaCocina(Array.Empty<Pedido>());
        Assert.True(cola.EstaVacia);
        Assert.Null(cola.TomarSiguiente());
        Assert.Null(cola.Siguiente);
    }

    // ------------------------------------------------------------------ RangoFechas (struct)

    [Fact]
    public void RangoFechas_FinalAnteriorAlInicio_Lanza()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        Assert.Throws<Ambrosia.Nucleo.Excepciones.ReglaNegocioException>(() => new RangoFechas(hoy, hoy.AddDays(-1)));
    }

    [Fact]
    public void RangoFechas_UltimosSieteDias_TieneSieteDias()
    {
        var rango = RangoFechas.UltimosDias(7);
        Assert.Equal(7, rango.TotalDias);
        Assert.Equal(rango.Hasta.AddDays(1).ToDateTime(TimeOnly.MinValue), rango.FinExclusivo);
    }
}
