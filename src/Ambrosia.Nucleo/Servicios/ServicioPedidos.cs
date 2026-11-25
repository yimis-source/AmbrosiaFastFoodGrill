// Registro de pedidos (valida y descuenta inventario), cambios de estado y cola de cocina.
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Estructuras;
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Nucleo.Servicios;

public class ServicioPedidos
{
    private readonly AmbrosiaDbContext _db;

    public ServicioInventario Inventario { get; }

    public ServicioPedidos(AmbrosiaDbContext db, ServicioInventario inventario)
    {
        _db = db;
        Inventario = inventario;
    }

    public async Task<Pedido> CrearPedidoAsync(SolicitudPedido solicitud, string usuario)
    {
        ValidarSolicitud(solicitud);

        var cantidades = new Dictionary<int, int>();
        var notas = new Dictionary<int, string>();
        foreach (var item in solicitud.Items.Where(i => i.Cantidad > 0))
        {
            cantidades[item.ProductoId] = cantidades.GetValueOrDefault(item.ProductoId) + item.Cantidad;
            if (!string.IsNullOrWhiteSpace(item.Nota))
                notas[item.ProductoId] = item.Nota.Trim();
        }

        var ids = cantidades.Keys.ToList();
        var productos = await _db.Productos
            .Include(p => p.Receta)
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var id in ids)
        {
            if (!productos.TryGetValue(id, out var producto))
                throw new EntidadNoEncontradaException("producto", id);
            if (!producto.Disponible)
                throw new ReglaNegocioException($"«{producto.Nombre}» no está disponible en este momento.");
        }

        var requerimientos = ServicioInventario.CalcularRequerimientos(
            ids.Select(id => (productos[id], cantidades[id])));
        var insumoIds = requerimientos.Keys.ToList();
        var insumos = await _db.Insumos
            .Where(i => insumoIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id);

        var faltantes = ServicioInventario.BuscarFaltantes(requerimientos, insumos);
        if (faltantes.Count > 0)
            throw new StockInsuficienteException(faltantes);

        var pedido = new Pedido
        {
            FechaCreacion = DateTime.Now,
            Tipo = solicitud.Tipo,
            MetodoPago = solicitud.MetodoPago,
            Estado = EstadoPedido.Pendiente,
            Mesa = solicitud.Tipo == TipoPedido.Mesa ? solicitud.Mesa : null,
            NombreCliente = Limpiar(solicitud.NombreCliente),
            Direccion = solicitud.Tipo == TipoPedido.Domicilio ? Limpiar(solicitud.Direccion) : null,
            Telefono = solicitud.Tipo == TipoPedido.Domicilio ? Limpiar(solicitud.Telefono) : null,
            Observaciones = Limpiar(solicitud.Observaciones),
            CreadoPor = usuario
        };

        foreach (var id in ids)
        {
            var producto = productos[id];
            pedido.Detalles.Add(new DetallePedido
            {
                ProductoId = id,
                NombreProducto = producto.Nombre,
                PrecioUnitario = producto.Precio,
                Cantidad = cantidades[id],
                Nota = notas.GetValueOrDefault(id)
            });
        }
        AplicarTotales(pedido);

        Inventario.Descontar(requerimientos, insumos);
        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync();
        return pedido;
    }

    public static void AplicarTotales(Pedido pedido)
    {
        ICalculadorCargo calculador = FabricaCargos.Para(pedido.Tipo);
        pedido.Subtotal = pedido.Detalles.Sum(d => d.Subtotal);
        pedido.Cargo = calculador.Calcular(pedido.Subtotal);
        pedido.Total = pedido.Subtotal + pedido.Cargo;
    }

    public static void ValidarSolicitud(SolicitudPedido solicitud)
    {
        var errores = new List<string>();

        if (solicitud.Items.Count == 0 || solicitud.Items.All(i => i.Cantidad <= 0))
            errores.Add("Agregue al menos un producto.");
        if (solicitud.Items.Any(i => i.Cantidad is < 0 or > 50))
            errores.Add("Las cantidades deben estar entre 1 y 50.");

        switch (solicitud.Tipo)
        {
            case TipoPedido.Mesa when solicitud.Mesa is null or < 1 or > 30:
                errores.Add("Indique un número de mesa entre 1 y 30.");
                break;
            case TipoPedido.ParaLlevar when string.IsNullOrWhiteSpace(solicitud.NombreCliente):
                errores.Add("Para llevar: escriba el nombre del cliente.");
                break;
            case TipoPedido.Domicilio when string.IsNullOrWhiteSpace(solicitud.NombreCliente)
                                         || string.IsNullOrWhiteSpace(solicitud.Direccion)
                                         || string.IsNullOrWhiteSpace(solicitud.Telefono):
                errores.Add("Domicilio: el nombre, la dirección y el teléfono son obligatorios.");
                break;
        }

        if (errores.Count > 0)
            throw new ReglaNegocioException(string.Join(" ", errores));
    }

    public static bool EsTransicionValida(EstadoPedido actual, EstadoPedido nuevo) => (actual, nuevo) switch
    {
        (EstadoPedido.Pendiente, EstadoPedido.EnPreparacion) => true,
        (EstadoPedido.Pendiente, EstadoPedido.Cancelado) => true,
        (EstadoPedido.EnPreparacion, EstadoPedido.Listo) => true,
        (EstadoPedido.Listo, EstadoPedido.Entregado) => true,
        _ => false
    };

    public async Task<Pedido> CambiarEstadoAsync(int id, EstadoPedido nuevo)
    {
        var pedido = await ObtenerAsync(id);
        if (!EsTransicionValida(pedido.Estado, nuevo))
            throw new TransicionInvalidaException(pedido.Estado, nuevo);

        if (nuevo == EstadoPedido.Cancelado)
            await DevolverInventarioAsync(pedido);

        pedido.Estado = nuevo;
        var ahora = DateTime.Now;
        switch (nuevo)
        {
            case EstadoPedido.EnPreparacion: pedido.FechaInicioPreparacion = ahora; break;
            case EstadoPedido.Listo: pedido.FechaListo = ahora; break;
            case EstadoPedido.Entregado: pedido.FechaEntrega = ahora; break;
        }

        await _db.SaveChangesAsync();
        return pedido;
    }

    public Task<Pedido> CancelarAsync(int id) => CambiarEstadoAsync(id, EstadoPedido.Cancelado);

    public Task<Pedido> MarcarListoAsync(int id) => CambiarEstadoAsync(id, EstadoPedido.Listo);

    public Task<Pedido> EntregarAsync(int id) => CambiarEstadoAsync(id, EstadoPedido.Entregado);

    private async Task DevolverInventarioAsync(Pedido pedido)
    {
        var ids = pedido.Detalles.Select(d => d.ProductoId).Distinct().ToList();
        var productos = await _db.Productos
            .Include(p => p.Receta)
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var requerimientos = ServicioInventario.CalcularRequerimientos(
            pedido.Detalles
                .Where(d => productos.ContainsKey(d.ProductoId))
                .Select(d => (productos[d.ProductoId], d.Cantidad)));

        var insumoIds = requerimientos.Keys.ToList();
        var insumos = await _db.Insumos.Where(i => insumoIds.Contains(i.Id)).ToListAsync();
        ServicioInventario.Devolver(requerimientos, insumos);
    }

    public async Task<Pedido> ObtenerAsync(int id) =>
        await _db.Pedidos
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(p => p.Id == id)
        ?? throw new EntidadNoEncontradaException("pedido", id);

    public async Task<List<Pedido>> ListarAsync(EstadoPedido? estado = null, DateOnly? dia = null)
    {
        IQueryable<Pedido> consulta = _db.Pedidos.Include(p => p.Detalles);

        if (estado is EstadoPedido filtro)
            consulta = consulta.Where(p => p.Estado == filtro);

        if (dia is DateOnly fecha)
        {
            var inicio = fecha.ToDateTime(TimeOnly.MinValue);
            var fin = inicio.AddDays(1);
            consulta = consulta.Where(p => p.FechaCreacion >= inicio && p.FechaCreacion < fin);
        }

        return await consulta.OrderByDescending(p => p.FechaCreacion).ToListAsync();
    }

    public Task<List<Pedido>> ListarPorEstadoAsync(EstadoPedido estado) =>
        _db.Pedidos
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .Where(p => p.Estado == estado)
            .OrderBy(p => p.FechaCreacion)
            .ToListAsync();

    public async Task<ColaCocina> ObtenerColaCocinaAsync() =>
        new ColaCocina(await ListarPorEstadoAsync(EstadoPedido.Pendiente));

    public async Task<Pedido?> TomarSiguienteAsync()
    {
        var cola = await ObtenerColaCocinaAsync();
        var pedido = cola.TomarSiguiente();
        if (pedido is null)
            return null;

        pedido.Estado = EstadoPedido.EnPreparacion;
        pedido.FechaInicioPreparacion = DateTime.Now;
        await _db.SaveChangesAsync();
        return pedido;
    }

    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
