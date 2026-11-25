// Productos de la carta y sus recetas (solo Administrador).
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ambrosia.Web.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class ProductosController : Controller
{
    private const string CamposProducto = "Id,Nombre,Descripcion,Precio,CategoriaId,Disponible,TiempoPreparacionMinutos";

    private readonly ServicioCatalogo _catalogo;
    private readonly ServicioInventario _inventario;

    public ProductosController(ServicioCatalogo catalogo, ServicioInventario inventario)
    {
        _catalogo = catalogo;
        _inventario = inventario;
    }

    public async Task<IActionResult> Index(int? categoriaId)
    {
        ViewBag.Categorias = new SelectList(await _catalogo.ListarCategoriasAsync(), "Id", "Nombre", categoriaId);
        ViewBag.CategoriaId = categoriaId;
        return View(await _catalogo.ListarProductosAsync(categoriaId));
    }

    public async Task<IActionResult> Detalle(int id)
    {
        try
        {
            var modelo = new ProductoDetalleViewModel
            {
                Producto = await _catalogo.ObtenerProductoAsync(id),
                Insumos = await _inventario.ListarAsync()
            };
            return View(modelo);
        }
        catch (EntidadNoEncontradaException)
        {
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarCategoriasAsync();
        return View("Formulario", new Producto());
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        try
        {
            var producto = await _catalogo.ObtenerProductoAsync(id);
            await CargarCategoriasAsync(producto.CategoriaId);
            return View("Formulario", producto);
        }
        catch (EntidadNoEncontradaException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([Bind(CamposProducto)] Producto producto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var nuevo = producto.EsNueva;
                await _catalogo.GuardarProductoAsync(producto);
                TempData["Exito"] = nuevo
                    ? $"«{producto.Nombre}» creado. Ahora agregue su receta."
                    : $"«{producto.Nombre}» actualizado.";
                return RedirectToAction(nameof(Detalle), new { id = producto.Id });
            }
            catch (ReglaNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        await CargarCategoriasAsync(producto.CategoriaId);
        return View("Formulario", producto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarDisponibilidad(int id)
    {
        var producto = await _catalogo.CambiarDisponibilidadAsync(id);
        TempData["Exito"] = producto.Disponible
            ? $"«{producto.Nombre}» vuelve a aparecer en la carta."
            : $"«{producto.Nombre}» se ocultó de la carta.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GuardarIngrediente(IngredienteFormulario formulario)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Detalle), new { id = formulario.ProductoId });
        }

        try
        {
            await _catalogo.GuardarIngredienteAsync(formulario.ProductoId, formulario.InsumoId, formulario.Cantidad);
            TempData["Exito"] = "Receta actualizada.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalle), new { id = formulario.ProductoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarIngrediente(int id, int productoId)
    {
        try
        {
            await _catalogo.QuitarIngredienteAsync(id);
            TempData["Exito"] = "Ingrediente quitado de la receta.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalle), new { id = productoId });
    }

    private async Task CargarCategoriasAsync(int? seleccionada = null) =>
        ViewBag.Categorias = new SelectList(await _catalogo.ListarCategoriasAsync(), "Id", "Nombre", seleccionada);
}
