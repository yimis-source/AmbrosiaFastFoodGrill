// Entidad Insumo: materia prima con stock actual, stock mínimo y costo.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ambrosia.Nucleo.Modelos;

public class Insumo : EntidadBase
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80, ErrorMessage = "Máximo 80 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    public UnidadMedida Unidad { get; set; } = UnidadMedida.Unidad;

    [Display(Name = "Stock actual")]
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "El stock no puede ser negativo.",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal StockActual { get; set; }

    [Display(Name = "Stock mínimo")]
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "El mínimo no puede ser negativo.",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal StockMinimo { get; set; }

    [Display(Name = "Costo por unidad de medida")]
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "El costo no puede ser negativo.",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal CostoUnitario { get; set; }

    [NotMapped]
    public bool BajoMinimo => StockActual <= StockMinimo;

    [NotMapped]
    public decimal NivelStock => StockMinimo <= 0 ? 99m : Math.Round(StockActual / StockMinimo, 2);

    [NotMapped]
    public decimal ValorEnBodega => StockActual * CostoUnitario;
}
