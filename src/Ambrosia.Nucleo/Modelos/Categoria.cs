using System.ComponentModel.DataAnnotations;

namespace Ambrosia.Nucleo.Modelos;

/// <summary>Sección de la carta: Hamburguesas, Perros, Asados, Acompañantes, Bebidas…</summary>
public class Categoria : EntidadBase
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Emoji que acompaña la categoría en la carta (🍔, 🌭, 🥩…).</summary>
    [StringLength(8)]
    public string Icono { get; set; } = "🍽️";

    /// <summary>Posición en la carta (1 = primero).</summary>
    [Range(1, 99, ErrorMessage = "El orden debe estar entre 1 y 99.")]
    public int Orden { get; set; } = 1;

    /// <summary>Productos de la categoría (relación uno a muchos).</summary>
    public List<Producto> Productos { get; set; } = new();
}
