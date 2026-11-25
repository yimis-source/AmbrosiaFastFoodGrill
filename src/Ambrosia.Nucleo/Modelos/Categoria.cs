// Entidad Categoria: sección de la carta.
using System.ComponentModel.DataAnnotations;

namespace Ambrosia.Nucleo.Modelos;

public class Categoria : EntidadBase
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, ErrorMessage = "Máximo 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(8)]
    public string Icono { get; set; } = "🍽️";

    [Range(1, 99, ErrorMessage = "El orden debe estar entre 1 y 99.")]
    public int Orden { get; set; } = 1;

    public List<Producto> Productos { get; set; } = new();
}
