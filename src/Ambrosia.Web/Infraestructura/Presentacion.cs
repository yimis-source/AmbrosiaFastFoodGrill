// Ayudas visuales para las vistas: colores de estado, íconos y barras.
using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Web.Infraestructura;

public static class Presentacion
{
    public static string ClaseEstado(EstadoPedido estado) => estado switch
    {
        EstadoPedido.Pendiente => "bg-warning text-dark",
        EstadoPedido.EnPreparacion => "bg-info text-dark",
        EstadoPedido.Listo => "bg-success",
        EstadoPedido.Entregado => "bg-secondary",
        EstadoPedido.Cancelado => "bg-danger",
        _ => "bg-light text-dark"
    };

    public static string IconoTipo(TipoPedido tipo) => tipo switch
    {
        TipoPedido.Mesa => "🍽️",
        TipoPedido.ParaLlevar => "🛍️",
        TipoPedido.Domicilio => "🛵",
        _ => "•"
    };

    public static string IconoPago(MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "💵",
        MetodoPago.Tarjeta => "💳",
        _ => "📱"
    };

    public static string PorcentajeBarra(decimal valor, decimal maximo)
    {
        var porcentaje = maximo <= 0 ? 0 : Math.Round(valor / maximo * 100, 1);
        return porcentaje.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string ClaseEspera(int minutos, int estimado) =>
        minutos <= estimado ? "text-success" : minutos <= estimado * 2 ? "text-warning" : "text-danger";
}
