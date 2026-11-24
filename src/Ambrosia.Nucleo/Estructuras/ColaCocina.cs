using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Estructuras;

/// <summary>
/// Cola de pedidos pendientes que ve la cocina.
///
/// ESTRUCTURA USADA: Queue&lt;Pedido&gt; (cola FIFO, "primero en llegar, primero en salir").
/// Es la regla justa de una cocina: el primer pedido registrado es el primero que se prepara.
/// Enqueue y Dequeue son O(1); con una List habría que hacer RemoveAt(0), que es O(n)
/// porque desplaza todos los elementos una posición.
///
/// Se descartó PriorityQueue (por ejemplo, priorizar domicilios) porque haría esperar
/// indefinidamente a las mesas cuando entran muchos domicilios seguidos.
/// </summary>
public class ColaCocina
{
    private readonly Queue<Pedido> _cola;

    public ColaCocina(IEnumerable<Pedido> pedidosPendientes)
    {
        // Se encolan en orden de llegada; si dos llegan en el mismo instante, decide el Id.
        _cola = new Queue<Pedido>(pedidosPendientes
            .OrderBy(p => p.FechaCreacion)
            .ThenBy(p => p.Id));
    }

    public int Cantidad => _cola.Count;
    public bool EstaVacia => _cola.Count == 0;

    /// <summary>El pedido que sigue, sin sacarlo de la cola (Peek). Null si la cola está vacía.</summary>
    public Pedido? Siguiente => _cola.Count > 0 ? _cola.Peek() : null;

    /// <summary>Saca el pedido que sigue (Dequeue). Null si la cola está vacía.</summary>
    public Pedido? TomarSiguiente() => _cola.Count > 0 ? _cola.Dequeue() : null;

    /// <summary>Recorre la cola en orden FIFO sin modificarla.</summary>
    public IEnumerable<Pedido> EnOrden => _cola;

    /// <summary>Posición 1, 2, 3… de un pedido en la cola (o -1 si no está).</summary>
    public int PosicionDe(int pedidoId)
    {
        var posicion = 1;
        foreach (var pedido in _cola)
        {
            if (pedido.Id == pedidoId) return posicion;
            posicion++;
        }
        return -1;
    }

    /// <summary>Minutos aproximados hasta despachar toda la cola (un pedido tras otro).</summary>
    public int MinutosParaVaciar => _cola.Sum(p => p.MinutosEstimados);
}
