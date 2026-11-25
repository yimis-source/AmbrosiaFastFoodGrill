// Cola FIFO (Queue) de pedidos pendientes: el primero que llega es el primero que se cocina.
using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Estructuras;

public class ColaCocina
{
    private readonly Queue<Pedido> _cola;

    public ColaCocina(IEnumerable<Pedido> pedidosPendientes)
    {
        _cola = new Queue<Pedido>(pedidosPendientes
            .OrderBy(p => p.FechaCreacion)
            .ThenBy(p => p.Id));
    }

    public int Cantidad => _cola.Count;
    public bool EstaVacia => _cola.Count == 0;

    public Pedido? Siguiente => _cola.Count > 0 ? _cola.Peek() : null;

    public Pedido? TomarSiguiente() => _cola.Count > 0 ? _cola.Dequeue() : null;

    public IEnumerable<Pedido> EnOrden => _cola;

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

    public int MinutosParaVaciar => _cola.Sum(p => p.MinutosEstimados);
}
