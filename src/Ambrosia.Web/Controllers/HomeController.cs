using System.Diagnostics;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ambrosia.Web.Controllers;

/// <summary>Página pública: la carta de Ambrosía (no requiere iniciar sesión).</summary>
[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ServicioCatalogo _catalogo;
    private readonly ServicioInventario _inventario;

    // Los servicios llegan por INYECCIÓN DE DEPENDENCIAS (registrados en Program.cs).
    public HomeController(ServicioCatalogo catalogo, ServicioInventario inventario)
    {
        _catalogo = catalogo;
        _inventario = inventario;
    }

    public async Task<IActionResult> Index()
    {
        var modelo = new CartaViewModel
        {
            Grupos = await _catalogo.ObtenerCartaAsync(),
            Agotados = await _inventario.ObtenerProductosAgotadosAsync()
        };
        return View(modelo);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
