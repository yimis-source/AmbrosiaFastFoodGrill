// Excepciones del negocio: stock insuficiente, entidad no encontrada y transición inválida.
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Utilidades;

namespace Ambrosia.Nucleo.Excepciones;

public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

public class StockInsuficienteException : ReglaNegocioException
{
    public IReadOnlyList<string> Faltantes { get; }

    public StockInsuficienteException(IReadOnlyList<string> faltantes)
        : base("No hay inventario suficiente: " + string.Join("; ", faltantes) + ".")
    {
        Faltantes = faltantes;
    }
}

public class EntidadNoEncontradaException : ReglaNegocioException
{
    public EntidadNoEncontradaException(string entidad, int id)
        : base($"No existe {entidad} con código {id}.") { }
}

public class TransicionInvalidaException : ReglaNegocioException
{
    public EstadoPedido Actual { get; }
    public EstadoPedido Solicitado { get; }

    public TransicionInvalidaException(EstadoPedido actual, EstadoPedido solicitado)
        : base($"Un pedido en estado «{actual.Texto()}» no puede pasar a «{solicitado.Texto()}».")
    {
        Actual = actual;
        Solicitado = solicitado;
    }
}
