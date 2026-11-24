using System.Globalization;
using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Utilidades;

/// <summary>
/// MÉTODOS DE EXTENSIÓN: agregan métodos a tipos existentes (decimal, enumeraciones, DateTime)
/// sin modificarlos. Se usan igual en la web y en el escritorio: precio.ComoPesos().
/// </summary>
public static class Extensiones
{
    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    /// <summary>18000 → "$ 18.000" (formato colombiano, igual en cualquier computador).</summary>
    public static string ComoPesos(this decimal valor)
    {
        if (valor < 0) return "-" + ComoPesos(-valor);
        var redondeado = Math.Round(valor, 0, MidpointRounding.AwayFromZero);
        return "$ " + FormatoColombiano(redondeado, "#,0");
    }

    /// <summary>1500.5 → "1.500,5".</summary>
    public static string ComoNumero(this decimal valor, int decimales = 2)
    {
        var formato = decimales <= 0 ? "#,0" : "#,0." + new string('#', decimales);
        return FormatoColombiano(valor, formato);
    }

    /// <summary>1500 + Gramo → "1.500 g".</summary>
    public static string ComoCantidad(this decimal valor, UnidadMedida unidad) =>
        $"{valor.ComoNumero()} {unidad.Abreviatura()}";

    public static string Abreviatura(this UnidadMedida unidad) => unidad switch
    {
        UnidadMedida.Gramo => "g",
        UnidadMedida.Mililitro => "ml",
        UnidadMedida.Unidad => "und",
        _ => string.Empty
    };

    // Texto amigable de cada enumeración (sobrecarga del mismo método para cada tipo).
    // EstadoPedido.EnPreparacion.Texto() → "En preparación".

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

    /// <summary>Todos los valores de una enumeración (método GENÉRICO con restricción "where T : Enum").</summary>
    public static T[] Valores<T>() where T : struct, Enum => Enum.GetValues<T>();

    /// <summary>"hace 3 min", "hace 2 h"…</summary>
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

    /// <summary>24/11/2026 → "mar 24/11" (sin depender del idioma instalado en el equipo).</summary>
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

    // En Colombia el separador de miles es "." y el decimal es ",": se formatea con la
    // cultura invariante (1,500.5) y luego se intercambian los separadores (1.500,5).
    private static string FormatoColombiano(decimal valor, string formato) =>
        valor.ToString(formato, Invariante)
             .Replace(",", "§")
             .Replace(".", ",")
             .Replace("§", ".");
}
