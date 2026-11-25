// Crea la base de datos y carga roles, usuarios de prueba, carta y pedidos de ejemplo.
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Nucleo.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Nucleo.Datos;

public static class InicializadorBaseDatos
{
    public static readonly (string Usuario, string Clave, string Rol)[] UsuariosIniciales =
    {
        ("admin", "Admin123*", Roles.Administrador),
        ("cajero", "Cajero123*", Roles.Cajero),
        ("cocina", "Cocina123*", Roles.Cocina),
    };

    public static async Task InicializarAsync(AmbrosiaDbContext db, bool incluirPedidosDeEjemplo = true)
    {
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        await SembrarSeguridadAsync(db);

        if (!await db.Categorias.AnyAsync())
            await SembrarNegocioAsync(db, incluirPedidosDeEjemplo);
    }

    private static async Task SembrarSeguridadAsync(AmbrosiaDbContext db)
    {
        foreach (var rol in Roles.Todos)
        {
            var normalizado = rol.ToUpperInvariant();
            if (!await db.Roles.AnyAsync(r => r.NormalizedName == normalizado))
                db.Roles.Add(new IdentityRole(rol) { NormalizedName = normalizado });
        }
        await db.SaveChangesAsync();

        var hasher = new PasswordHasher<IdentityUser>();
        foreach (var (usuario, clave, rol) in UsuariosIniciales)
        {
            var normalizado = usuario.ToUpperInvariant();
            if (await db.Users.AnyAsync(u => u.NormalizedUserName == normalizado)) continue;

            var correo = $"{usuario}@ambrosia.co";
            var nuevo = new IdentityUser(usuario)
            {
                NormalizedUserName = normalizado,
                Email = correo,
                NormalizedEmail = correo.ToUpperInvariant(),
                EmailConfirmed = true,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString("N").ToUpperInvariant()
            };
            nuevo.PasswordHash = hasher.HashPassword(nuevo, clave);
            db.Users.Add(nuevo);

            var nombreRol = rol.ToUpperInvariant();
            var rolId = (await db.Roles.FirstAsync(r => r.NormalizedName == nombreRol)).Id;
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = nuevo.Id, RoleId = rolId });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SembrarNegocioAsync(AmbrosiaDbContext db, bool incluirPedidos)
    {
        var categorias = DatosSemilla.Categorias
            .Select(c => new Categoria { Nombre = c.Nombre, Icono = c.Icono, Orden = c.Orden })
            .ToDictionary(c => c.Nombre);
        db.Categorias.AddRange(categorias.Values);

        var insumos = DatosSemilla.Insumos
            .Select(i => new Insumo
            {
                Nombre = i.Nombre,
                Unidad = i.Unidad,
                StockActual = i.Stock,
                StockMinimo = i.Minimo,
                CostoUnitario = i.Costo
            })
            .ToDictionary(i => i.Nombre);
        db.Insumos.AddRange(insumos.Values);

        var productos = new List<Producto>();
        foreach (var semilla in DatosSemilla.Productos)
        {
            var producto = new Producto
            {
                Nombre = semilla.Nombre,
                Descripcion = semilla.Descripcion,
                Precio = semilla.Precio,
                Categoria = categorias[semilla.Categoria],
                TiempoPreparacionMinutos = semilla.Minutos,
                Disponible = true
            };
            foreach (var (insumo, cantidad) in semilla.Receta)
                producto.Receta.Add(new RecetaItem { Insumo = insumos[insumo], Cantidad = cantidad });
            productos.Add(producto);
        }
        db.Productos.AddRange(productos);
        await db.SaveChangesAsync();

        if (incluirPedidos)
        {
            db.Pedidos.AddRange(GenerarPedidosDeEjemplo(productos));
            await db.SaveChangesAsync();
        }
    }

    private static List<Pedido> GenerarPedidosDeEjemplo(List<Producto> productos)
    {
        var azar = new Random(2026);
        var ahora = DateTime.Now;
        var hoy = DateOnly.FromDateTime(ahora);
        var pedidos = new List<Pedido>();

        for (var diasAtras = 7; diasAtras >= 1; diasAtras--)
        {
            var dia = hoy.AddDays(-diasAtras);
            var cantidad = azar.Next(6, 11);
            for (var i = 0; i < cantidad; i++)
            {
                var hora = dia.ToDateTime(new TimeOnly(11, 30)).AddMinutes(azar.Next(0, 600));
                var estado = azar.Next(12) == 0 ? EstadoPedido.Cancelado : EstadoPedido.Entregado;
                pedidos.Add(CrearPedidoAleatorio(azar, productos, hora, estado));
            }
        }

        (int Minutos, EstadoPedido Estado)[] deHoy =
        {
            (150, EstadoPedido.Entregado), (115, EstadoPedido.Entregado), (80, EstadoPedido.Entregado),
            (35, EstadoPedido.Listo), (22, EstadoPedido.EnPreparacion),
            (14, EstadoPedido.Pendiente), (9, EstadoPedido.Pendiente), (4, EstadoPedido.Pendiente)
        };
        foreach (var (minutos, estado) in deHoy)
            pedidos.Add(CrearPedidoAleatorio(azar, productos, ahora.AddMinutes(-minutos), estado));

        return pedidos;
    }

    private static Pedido CrearPedidoAleatorio(Random azar, List<Producto> productos, DateTime fecha, EstadoPedido estado)
    {
        var tipo = azar.Next(100) switch
        {
            < 50 => TipoPedido.Mesa,
            < 75 => TipoPedido.ParaLlevar,
            _ => TipoPedido.Domicilio
        };
        var pago = azar.Next(100) switch
        {
            < 35 => MetodoPago.Efectivo,
            < 65 => MetodoPago.Nequi,
            < 90 => MetodoPago.Tarjeta,
            _ => MetodoPago.Daviplata
        };
        var cliente = DatosSemilla.Clientes[azar.Next(DatosSemilla.Clientes.Length)];

        var pedido = new Pedido
        {
            FechaCreacion = fecha,
            Tipo = tipo,
            MetodoPago = pago,
            Estado = estado,
            Mesa = tipo == TipoPedido.Mesa ? azar.Next(1, 13) : null,
            NombreCliente = tipo == TipoPedido.Mesa ? null : cliente,
            Direccion = tipo == TipoPedido.Domicilio
                ? DatosSemilla.Direcciones[azar.Next(DatosSemilla.Direcciones.Length)]
                : null,
            Telefono = tipo == TipoPedido.Domicilio ? $"3{azar.Next(100_000_000, 199_999_999)}" : null,
            CreadoPor = azar.Next(4) == 0 ? "admin" : "cajero",
            FechaInicioPreparacion = estado is EstadoPedido.Pendiente or EstadoPedido.Cancelado ? null : fecha.AddMinutes(3),
            FechaListo = estado is EstadoPedido.Listo or EstadoPedido.Entregado ? fecha.AddMinutes(18) : null,
            FechaEntrega = estado == EstadoPedido.Entregado ? fecha.AddMinutes(22) : null
        };

        var elegidos = new HashSet<int>();
        var lineas = azar.Next(1, 5);
        while (elegidos.Count < lineas)
            elegidos.Add(azar.Next(productos.Count));

        foreach (var indice in elegidos)
        {
            var producto = productos[indice];
            pedido.Detalles.Add(new DetallePedido
            {
                Producto = producto,
                NombreProducto = producto.Nombre,
                PrecioUnitario = producto.Precio,
                Cantidad = azar.Next(1, 3)
            });
        }

        ServicioPedidos.AplicarTotales(pedido);
        return pedido;
    }
}
