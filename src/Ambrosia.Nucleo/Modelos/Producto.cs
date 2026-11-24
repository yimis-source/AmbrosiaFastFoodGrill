using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ambrosia.Nucleo.Modelos;

/// <summary>Plato o bebida que se vende en la carta.</summary>
public class Producto : EntidadBase
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(250, ErrorMessage = "Máximo 250 caracteres.")]
    public string? Descripcion { get; set; }

    [Range(typeof(decimal), "500", "500000", ErrorMessage = "El precio debe estar entre $500 y $500.000.",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Precio { get; set; }

    [Display(Name = "Categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int CategoriaId { get; set; }

    public Categoria? Categoria { get; set; }

    [Display(Name = "Disponible en la carta")]
    public bool Disponible { get; set; } = true;

    [Display(Name = "Tiempo de preparación (min)")]
    [Range(1, 90, ErrorMessage = "Entre 1 y 90 minutos.")]
    public int TiempoPreparacionMinutos { get; set; } = 10;

    /// <summary>
    /// Ingredientes que consume una unidad del producto. Con la receta se descuenta
    /// el inventario automáticamente cada vez que se vende.
    /// </summary>
    public List<RecetaItem> Receta { get; set; } = new();

    /// <summary>Costo de los insumos de una unidad (requiere la receta cargada con sus insumos).</summary>
    [NotMapped]
    public decimal CostoReceta => Receta.Sum(r => r.Cantidad * (r.Insumo?.CostoUnitario ?? 0));

    /// <summary>Porcentaje del precio que queda después de pagar los insumos.</summary>
    [NotMapped]
    public decimal MargenPorcentaje => Precio <= 0 ? 0 : Math.Round((Precio - CostoReceta) / Precio * 100, 1);
}
