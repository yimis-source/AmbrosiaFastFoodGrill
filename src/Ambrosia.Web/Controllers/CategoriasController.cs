using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Nucleo.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambrosia.Web.Controllers;

/// <summary>CRUD de las secciones de la carta.</summary>
[Authorize(Roles = Roles.Administrador)]
public class CategoriasController : Controller
{
    private readonly ServicioCatalogo _catalogo;

    public CategoriasController(ServicioCatalogo catalogo) => _catalogo = catalogo;

    public async Task<IActionResult> Index() => View(await _catalogo.ListarCategoriasAsync());

    [HttpGet]
    public IActionResult Crear() => View("Formulario", new Categoria { Orden = 6 });

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        try
        {
            return View("Formulario", await _catalogo.ObtenerCategoriaAsync(id));
        }
        catch (EntidadNoEncontradaException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([Bind("Id,Nombre,Icono,Orden")] Categoria categoria)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _catalogo.GuardarCategoriaAsync(categoria);
                TempData["Exito"] = $"Categoría «{categoria.Nombre}» guardada.";
                return RedirectToAction(nameof(Index));
            }
            catch (ReglaNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        return View("Formulario", categoria);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _catalogo.EliminarCategoriaAsync(id);
            TempData["Exito"] = "Categoría eliminada.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
