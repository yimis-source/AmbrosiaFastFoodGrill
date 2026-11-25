// Métodos de extensión de formato: pesos, cantidades y textos de las enumeraciones.
using System.Globalization;
using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Utilidades;

public static class Extensiones
{
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    public static string ComoPesos(this decimal valor)
    {
        if (valor < 0) return "-" + ComoPesos(-valor);
        var redondeado = Math.Round(valor, 0, MidpointRounding.AwayFromZero);
        return "$ " + FormatoColombiano(redondeado, "#,0");
    }

    public static string ComoNumero(this decimal valor, int decimales = 2)
    {
        var formato = decimales <= 0 ? "#,0" : "#,0." + new string('#', decimales);
        return FormatoColombiano(valor, formato);
    }

    public static string ComoCantidad(this decimal valor, UnidadMedida unidad) =>
        $"{valor.ComoNumero()} {unidad.Abreviatura()}";

    public static string Abreviatura(this UnidadMedida unidad) => unidad switch
    {
        UnidadMedida.Gramo => "g",
        UnidadMedida.Mililitro => "ml",
        UnidadMedida.Unidad => "und",
        _ => string.Empty
    };

    public static string Texto(this EstadoPedido estado) => estado switch
    {
        EstadoPedido.Pendiente => "Pendiente",
        EstadoPedido.EnPreparacion => "En preparación",
        EstadoPedido.Listo => "Listo",
        EstadoPedido.Entregado => "Entregado",
        EstadoPedido.Cancelado => "Cancelado",
        _ => estado.ToString()
    };

    public static string Texto(this TipoPedido tipo) => tipo switch
    {
        TipoPedido.Mesa => "Mesa",
        TipoPedido.ParaLlevar => "Para llevar",
        TipoPedido.Domicilio => "Domicilio",
        _ => tipo.ToString()
    };

    public static string Texto(this MetodoPago metodo) => metodo switch
    {
        MetodoPago.Efectivo => "Efectivo",
        MetodoPago.Tarjeta => "Tarjeta",
        MetodoPago.Nequi => "Nequi",
        MetodoPago.Daviplata => "Daviplata",
        _ => metodo.ToString()
    };

    public static string Texto(this UnidadMedida unidad) => unidad switch
    {
        UnidadMedida.Gramo => "Gramos",
        UnidadMedida.Mililitro => "Mililitros",
        UnidadMedida.Unidad => "Unidades",
        _ => unidad.ToString()
    };

    public static T[] Valores<T>() where T : struct, Enum => Enum.GetValues<T>();

    public static string HaceCuanto(this DateTime fecha, DateTime ahora)
    {
        var diferencia = ahora - fecha;
        return diferencia.TotalMinutes switch
        {
            < 1 => "hace un momento",
            < 60 => $"hace {(int)diferencia.TotalMinutes} min",
            < 1440 => $"hace {(int)diferencia.TotalHours} h",
            _ => fecha.ToString("dd/MM/yyyy HH:mm", Invariante)
        };
    }

    public static string FechaCorta(this DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Invariante);

    public static string DiaCorto(this DateOnly dia)
    {
        var nombre = dia.DayOfWeek switch
        {
            DayOfWeek.Monday => "lun",
            DayOfWeek.Tuesday => "mar",
            DayOfWeek.Wednesday => "mié",
            DayOfWeek.Thursday => "jue",
            DayOfWeek.Friday => "vie",
            DayOfWeek.Saturday => "sáb",
            _ => "dom"
        };
        return $"{nombre} {dia.ToString("dd/MM", Invariante)}";
    }

    private static string FormatoColombiano(decimal valor, string formato) =>
        valor.ToString(formato, Invariante)
             .Replace(",", "§")
             .Replace(".", ",")
             .Replace("§", ".");
}
