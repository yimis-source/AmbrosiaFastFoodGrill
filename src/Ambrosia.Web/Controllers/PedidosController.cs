using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Nucleo.Utilidades;
using Ambrosia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambrosia.Web.Controllers;

/// <summary>Caja: registrar, consultar, entregar y cancelar pedidos.</summary>
[Authorize(Roles = Roles.AdministradorOCajero)]
public class PedidosController : Controller
{
    private readonly ServicioPedidos _pedidos;
    private readonly ServicioCatalogo _catalogo;

    public PedidosController(ServicioPedidos pedidos, ServicioCatalogo catalogo)
    {
        _pedidos = pedidos;
        _catalogo = catalogo;
    }

    // GET /Pedidos?estado=Listo&fecha=2026-11-24
    public async Task<IActionResult> Index(EstadoPedido? estado, DateOnly? fecha)
    {
        var dia = fecha ?? DateOnly.FromDateTime(DateTime.Now);
        var modelo = new PedidosIndexViewModel
        {
            Estado = estado,
            Fecha = dia,
            Pedidos = await _pedidos.ListarAsync(estado, dia)
        };
        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        var modelo = new CrearPedidoViewModel();
        await CargarCartaAsync(modelo);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearPedidoViewModel modelo)
    {
        if (ModelState.IsValid)
        {
            // Se escucha el EVENTO StockBajo para avisarle al cajero qué insumos hay que comprar.
            var avisos = new List<string>();
            _pedidos.Inventario.StockBajo += (_, e) => avisos.Add(e.Mensaje);

            try
            {
                var pedido = await _pedidos.CrearPedidoAsync(modelo.ComoSolicitud(), User.Identity?.Name ?? "web");
                TempData["Exito"] = $"Pedido {pedido.Codigo} registrado por {pedido.Total.ComoPesos()}. Ya está en la cola de cocina.";
                if (avisos.Count > 0)
                    TempData["Aviso"] = "Inventario bajo: " + string.Join(" ", avisos);
                return RedirectToAction(nameof(Detalle), new { id = pedido.Id });
            }
            catch (ReglaNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        await CargarCartaAsync(modelo);
        return View(modelo);
    }

    public async Task<IActionResult> Detalle(int id)
    {
        try
        {
            return View(await _pedidos.ObtenerAsync(id));
        }
        catch (EntidadNoEncontradaException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Entregar(int id) =>
        EjecutarAsync(id, () => _pedidos.EntregarAsync(id), p => $"Pedido {p.Codigo} entregado.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Cancelar(int id) =>
        EjecutarAsync(id, () => _pedidos.CancelarAsync(id), p => $"Pedido {p.Codigo} cancelado; los insumos volvieron al inventario.");

    /// <summary>
    /// Ejecuta una acción sobre un pedido y muestra el resultado.
    /// Recibe DELEGADOS (Func) para no repetir el mismo try/catch en cada acción.
    /// </summary>
    private async Task<IActionResult> EjecutarAsync(int id, Func<Task<Pedido>> accion, Func<Pedido, string> mensaje)
    {
        try
        {
            var pedido = await accion();
            TempData["Exito"] = mensaje(pedido);
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task CargarCartaAsync(CrearPedidoViewModel modelo)
    {
        modelo.Grupos = await _catalogo.ObtenerCartaAsync();
        modelo.Agotados = await _pedidos.Inventario.ObtenerProductosAgotadosAsync();
    }
}
