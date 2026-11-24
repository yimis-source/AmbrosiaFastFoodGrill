using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Servicios;

// ABSTRACCIÓN + HERENCIA + POLIMORFISMO (mismo esquema que ISensor / Sensor / SensorTemperatura):
//  • ICalculadorCargo es el contrato: el servicio de pedidos solo conoce la interfaz.
//  • CalculadorCargo es la clase base abstracta: define una Descripcion "virtual" por defecto
//    y obliga a cada subclase a implementar Calcular (abstract).
//  • Cada tipo de pedido redefine (override) el comportamiento. El método que se ejecuta
//    depende del tipo real del objeto, no del tipo de la variable.
// Si mañana Ambrosía cobra empaque para llevar, se agrega una subclase sin tocar el resto.

public interface ICalculadorCargo
{
    string Descripcion { get; }
    decimal Calcular(decimal subtotal);
}

/// <summary>Clase base abstracta de todos los cargos.</summary>
public abstract class CalculadorCargo : ICalculadorCargo
{
    /// <summary>Texto por defecto; las subclases pueden redefinirlo con override.</summary>
    public virtual string Descripcion => "Sin cargo adicional";

    /// <summary>Cada subclase está obligada a decir cómo calcula su cargo.</summary>
    public abstract decimal Calcular(decimal subtotal);

    public override string ToString() => Descripcion;
}

/// <summary>Para llevar: no tiene cargo adicional (usa la Descripcion de la clase base).</summary>
public sealed class SinCargo : CalculadorCargo
{
    public override decimal Calcular(decimal subtotal) => 0m;
}

/// <summary>Mesa: servicio voluntario sugerido del 10 %.</summary>
public sealed class CargoServicioMesa : CalculadorCargo
{
    public const decimal Porcentaje = 0.10m;

    public override string Descripcion => "Servicio voluntario (10 %)";

    public override decimal Calcular(decimal subtotal) =>
        Math.Round(subtotal * Porcentaje, 0, MidpointRounding.AwayFromZero);
}

/// <summary>Domicilio: tarifa fija, gratis desde cierto valor de compra.</summary>
public sealed class CargoDomicilio : CalculadorCargo
{
    public const decimal Tarifa = 4_000m;
    public const decimal MinimoEnvioGratis = 80_000m;

    public override string Descripcion => "Domicilio (gratis desde $ 80.000)";

    public override decimal Calcular(decimal subtotal) =>
        subtotal >= MinimoEnvioGratis ? 0m : Tarifa;
}

/// <summary>Fábrica: elige la subclase según el tipo de pedido (switch de expresiones).</summary>
public static class FabricaCargos
{
    public static CalculadorCargo Para(TipoPedido tipo) => tipo switch
    {
        TipoPedido.Mesa => new CargoServicioMesa(),
        TipoPedido.Domicilio => new CargoDomicilio(),
        _ => new SinCargo()
    };
}
