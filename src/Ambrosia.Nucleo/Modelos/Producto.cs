// Entidad Producto: plato o bebida de la carta, con su receta, costo y margen.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ambrosia.Nucleo.Modelos;

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

    public List<RecetaItem> Receta { get; set; } = new();

    [NotMapped]
    public decimal CostoReceta => Receta.Sum(r => r.Cantidad * (r.Insumo?.CostoUnitario ?? 0));

    [NotMapped]
    public decimal MargenPorcentaje => Precio <= 0 ? 0 : Math.Round((Precio - CostoReceta) / Precio * 100, 1);
}
