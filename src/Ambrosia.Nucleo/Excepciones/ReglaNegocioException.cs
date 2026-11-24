using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Utilidades;

namespace Ambrosia.Nucleo.Excepciones;

// Jerarquía de EXCEPCIONES PROPIAS. Las interfaces (web y escritorio) capturan
// ReglaNegocioException y muestran el mensaje al usuario; cualquier otra excepción
// se considera un error inesperado.

/// <summary>Se lanza cuando una operación viola una regla del negocio.</summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje) { }
}

/// <summary>No alcanza el inventario para preparar el pedido.</summary>
public class StockInsuficienteException : ReglaNegocioException
{
    public IReadOnlyList<string> Faltantes { get; }

    public StockInsuficienteException(IReadOnlyList<string> faltantes)
        : base("No hay inventario suficiente: " + string.Join("; ", faltantes) + ".")
    {
        Faltantes = faltantes;
    }
}

/// <summary>Se buscó un registro que no existe.</summary>
public class EntidadNoEncontradaException : ReglaNegocioException
{
    public EntidadNoEncontradaException(string entidad, int id)
        : base($"No existe {entidad} con código {id}.") { }
}

/// <summary>Cambio de estado no permitido (por ejemplo, entregar un pedido que no está listo).</summary>
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
