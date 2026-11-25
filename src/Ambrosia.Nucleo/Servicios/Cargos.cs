// Cargo adicional por tipo de pedido (interfaz, clase abstracta y polimorfismo).
using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Servicios;

public interface ICalculadorCargo
{
    string Descripcion { get; }
    decimal Calcular(decimal subtotal);
}

public abstract class CalculadorCargo : ICalculadorCargo
{
    public virtual string Descripcion => "Sin cargo adicional";

    public abstract decimal Calcular(decimal subtotal);

    public override string ToString() => Descripcion;
}

public sealed class SinCargo : CalculadorCargo
{
    public override decimal Calcular(decimal subtotal) => 0m;
}

public sealed class CargoServicioMesa : CalculadorCargo
{
    public const decimal Porcentaje = 0.10m;

    public override string Descripcion => "Servicio voluntario (10 %)";

    public override decimal Calcular(decimal subtotal) =>
        Math.Round(subtotal * Porcentaje, 0, MidpointRounding.AwayFromZero);
}

public sealed class CargoDomicilio : CalculadorCargo
{
    public const decimal Tarifa = 4_000m;
    public const decimal MinimoEnvioGratis = 80_000m;

    public override string Descripcion => "Domicilio (gratis desde $ 80.000)";

    public override decimal Calcular(decimal subtotal) =>
        subtotal >= MinimoEnvioGratis ? 0m : Tarifa;
}

public static class FabricaCargos
{
    public static CalculadorCargo Para(TipoPedido tipo) => tipo switch
    {
        TipoPedido.Mesa => new CargoServicioMesa(),
        TipoPedido.Domicilio => new CargoDomicilio(),
        _ => new SinCargo()
    };
}
