using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Nucleo.Utilidades;
using Ambrosia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambrosia.Web.Controllers;

/// <summary>Inventario: insumos, compras (reabastecer) y alertas de stock.</summary>
[Authorize(Roles = Roles.Administrador)]
public class InsumosController : Controller
{
    private const string CamposInsumo = "Id,Nombre,Unidad,StockActual,StockMinimo,CostoUnitario";

    private readonly ServicioInventario _inventario;

    public InsumosController(ServicioInventario inventario) => _inventario = inventario;

    public async Task<IActionResult> Index()
    {
        var modelo = new InsumosIndexViewModel
        {
            Insumos = await _inventario.ListarAsync(),
            Alertas = await _inventario.ObtenerAlertasAsync()
        };
        return View(modelo);
    }

    [HttpGet]
    public IActionResult Crear() => View("Formulario", new Insumo { Unidad = UnidadMedida.Gramo });

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        try
        {
            return View("Formulario", await _inventario.ObtenerAsync(id));
        }
        catch (EntidadNoEncontradaException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar([Bind(CamposInsumo)] Insumo insumo)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (insumo.EsNueva)
                    await _inventario.CrearAsync(insumo);
                else
                    await _inventario.ActualizarAsync(insumo);
                TempData["Exito"] = $"Insumo «{insumo.Nombre}» guardado.";
                return RedirectToAction(nameof(Index));
            }
            catch (ReglaNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        return View("Formulario", insumo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reabastecer(int id, decimal cantidad)
    {
        try
        {
            var insumo = await _inventario.ReabastecerAsync(id, cantidad);
            TempData["Exito"] = $"Ingresaron {cantidad.ComoCantidad(insumo.Unidad)} de «{insumo.Nombre}». " +
                                $"Nuevo stock: {insumo.StockActual.ComoCantidad(insumo.Unidad)}.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            await _inventario.EliminarAsync(id);
            TempData["Exito"] = "Insumo eliminado.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
