using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Ambrosia.Nucleo.Servicios;

/// <summary>Carta del restaurante: categorías, productos y recetas.</summary>
public class ServicioCatalogo
{
    private readonly AmbrosiaDbContext _db;

    public ServicioCatalogo(AmbrosiaDbContext db) => _db = db;

    // ================================================================== Carta

    /// <summary>
    /// Productos agrupados por categoría en el orden de la carta (LINQ GroupBy).
    /// Dentro de cada categoría se ordenan por precio.
    /// </summary>
    public async Task<List<GrupoMenu>> ObtenerCartaAsync(bool soloDisponibles = true)
    {
        var productos = await _db.Productos
            .Include(p => p.Categoria)
            .Where(p => !soloDisponibles || p.Disponible)
            .ToListAsync();

        return productos
            .GroupBy(p => p.CategoriaId)
            .Select(g => new GrupoMenu(g.First().Categoria!, g.OrderBy(p => p.Precio).ThenBy(p => p.Nombre).ToList()))
            .OrderBy(g => g.Categoria.Orden)
            .ThenBy(g => g.Categoria.Nombre)
            .ToList();
    }

    // ================================================================== Categorías

    public Task<List<Categoria>> ListarCategoriasAsync() =>
        _db.Categorias
            .Include(c => c.Productos)
            .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
            .ToListAsync();

    public async Task<Categoria> ObtenerCategoriaAsync(int id) =>
        await _db.Categorias.Include(c => c.Productos).FirstOrDefaultAsync(c => c.Id == id)
        ?? throw new EntidadNoEncontradaException("categoría", id);

    /// <summary>Crea (si Id = 0) o actualiza una categoría.</summary>
    public async Task GuardarCategoriaAsync(Categoria datos)
    {
        var nombre = datos.Nombre.Trim();
        if (await _db.Categorias.AnyAsync(c => c.Nombre == nombre && c.Id != datos.Id))
            throw new ReglaNegocioException($"Ya existe la categoría «{nombre}».");

        if (datos.EsNueva)
        {
            datos.Nombre = nombre;
            _db.Categorias.Add(datos);
        }
        else
        {
            var categoria = await ObtenerCategoriaAsync(datos.Id);
            categoria.Nombre = nombre;
            categoria.Icono = datos.Icono;
            categoria.Orden = datos.Orden;
        }
        await _db.SaveChangesAsync();
    }

    public async Task EliminarCategoriaAsync(int id)
    {
        var categoria = await ObtenerCategoriaAsync(id);
        if (categoria.Productos.Count > 0)
            throw new ReglaNegocioException($"«{categoria.Nombre}» tiene {categoria.Productos.Count} producto(s); muévalos o elimínelos primero.");
        _db.Categorias.Remove(categoria);
        await _db.SaveChangesAsync();
    }

    // ================================================================== Productos

    public async Task<List<Producto>> ListarProductosAsync(int? categoriaId = null)
    {
        IQueryable<Producto> consulta = _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Receta).ThenInclude(r => r.Insumo);

        if (categoriaId is int id)
            consulta = consulta.Where(p => p.CategoriaId == id);

        var productos = await consulta.ToListAsync();
        return productos
            .OrderBy(p => p.Categoria?.Orden)
            .ThenBy(p => p.Nombre)
            .ToList();
    }

    public async Task<Producto> ObtenerProductoAsync(int id) =>
        await _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Receta).ThenInclude(r => r.Insumo)
            .FirstOrDefaultAsync(p => p.Id == id)
        ?? throw new EntidadNoEncontradaException("producto", id);

    /// <summary>Crea (si Id = 0) o actualiza los datos generales de un producto.</summary>
    public async Task GuardarProductoAsync(Producto datos)
    {
        var nombre = datos.Nombre.Trim();
        if (await _db.Productos.AnyAsync(p => p.Nombre == nombre && p.Id != datos.Id))
            throw new ReglaNegocioException($"Ya existe un producto llamado «{nombre}».");
        if (!await _db.Categorias.AnyAsync(c => c.Id == datos.CategoriaId))
            throw new EntidadNoEncontradaException("categoría", datos.CategoriaId);

        if (datos.EsNueva)
        {
            datos.Nombre = nombre;
            _db.Productos.Add(datos);
        }
        else
        {
            var producto = await ObtenerProductoAsync(datos.Id);
            producto.Nombre = nombre;
            producto.Descripcion = datos.Descripcion;
            producto.Precio = datos.Precio;
            producto.CategoriaId = datos.CategoriaId;
            producto.Disponible = datos.Disponible;
            producto.TiempoPreparacionMinutos = datos.TiempoPreparacionMinutos;
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>Muestra u oculta el producto en la carta (no se borra para conservar el historial de ventas).</summary>
    public async Task<Producto> CambiarDisponibilidadAsync(int id)
    {
        var producto = await ObtenerProductoAsync(id);
        producto.Disponible = !producto.Disponible;
        await _db.SaveChangesAsync();
        return producto;
    }

    // ================================================================== Recetas

    /// <summary>Agrega un insumo a la receta; si ya estaba, actualiza la cantidad.</summary>
    public async Task GuardarIngredienteAsync(int productoId, int insumoId, decimal cantidad)
    {
        if (cantidad <= 0)
            throw new ReglaNegocioException("La cantidad del ingrediente debe ser mayor que cero.");

        var producto = await ObtenerProductoAsync(productoId);
        if (!await _db.Insumos.AnyAsync(i => i.Id == insumoId))
            throw new EntidadNoEncontradaException("insumo", insumoId);

        var existente = producto.Receta.FirstOrDefault(r => r.InsumoId == insumoId);
        if (existente is not null)
            existente.Cantidad = cantidad;
        else
            _db.RecetaItems.Add(new RecetaItem { ProductoId = productoId, InsumoId = insumoId, Cantidad = cantidad });

        await _db.SaveChangesAsync();
    }

    public async Task QuitarIngredienteAsync(int recetaItemId)
    {
        var item = await _db.RecetaItems.FirstOrDefaultAsync(r => r.Id == recetaItemId)
                   ?? throw new EntidadNoEncontradaException("ingrediente", recetaItemId);
        _db.RecetaItems.Remove(item);
        await _db.SaveChangesAsync();
    }
}
