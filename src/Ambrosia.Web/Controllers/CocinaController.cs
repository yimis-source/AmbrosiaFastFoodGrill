// Pantalla de cocina: cola FIFO, tomar el siguiente y marcar listo (Administrador y Cocina).
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambrosia.Web.Controllers;

[Authorize(Roles = Roles.AdministradorOCocina)]
public class CocinaController : Controller
{
    private readonly ServicioPedidos _pedidos;

    public CocinaController(ServicioPedidos pedidos) => _pedidos = pedidos;

    public async Task<IActionResult> Index()
    {
        var modelo = new CocinaViewModel
        {
            Cola = await _pedidos.ObtenerColaCocinaAsync(),
            EnPreparacion = await _pedidos.ListarPorEstadoAsync(EstadoPedido.EnPreparacion),
            Listos = await _pedidos.ListarPorEstadoAsync(EstadoPedido.Listo),
            Ahora = DateTime.Now
        };
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TomarSiguiente()
    {
        var pedido = await _pedidos.TomarSiguienteAsync();
        TempData[pedido is null ? "Aviso" : "Exito"] = pedido is null
            ? "No hay pedidos en cola."
            : $"Preparando {pedido.Codigo} ({pedido.Destino}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarListo(int id)
    {
        try
        {
            var pedido = await _pedidos.MarcarListoAsync(id);
            TempData["Exito"] = $"{pedido.Codigo} está listo para entregar.";
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
