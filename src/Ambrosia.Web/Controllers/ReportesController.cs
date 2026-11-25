// Panel de ventas por rango de fechas (solo Administrador).
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;
using Ambrosia.Nucleo.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambrosia.Web.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class ReportesController : Controller
{
    private readonly ServicioReportes _reportes;
    private readonly ServicioInventario _inventario;

    public ReportesController(ServicioReportes reportes, ServicioInventario inventario)
    {
        _reportes = reportes;
        _inventario = inventario;
    }

    public async Task<IActionResult> Index(DateOnly? desde, DateOnly? hasta)
    {
        RangoFechas rango;
        try
        {
            var ultimaSemana = RangoFechas.UltimosDias(7);
            rango = new RangoFechas(desde ?? ultimaSemana.Desde, hasta ?? ultimaSemana.Hasta);
        }
        catch (ReglaNegocioException ex)
        {
            TempData["Error"] = ex.Message;
            rango = RangoFechas.UltimosDias(7);
        }

        ViewBag.Alertas = await _inventario.ObtenerAlertasAsync(maximo: 3);
        return View(await _reportes.GenerarAsync(rango));
    }
}
