using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ambrosia.Nucleo.Modelos;

/// <summary>Materia prima del inventario: carne, pan, queso, papa, gaseosas…</summary>
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

    /// <summary>Verdadero cuando hay que volver a comprar.</summary>
    [NotMapped]
    public bool BajoMinimo => StockActual <= StockMinimo;

    /// <summary>
    /// Relación stock/mínimo: 1 = justo en el mínimo, 0.5 = la mitad del mínimo.
    /// Entre más bajo, más urgente (se usa como prioridad en la PriorityQueue de alertas).
    /// </summary>
    [NotMapped]
    public decimal NivelStock => StockMinimo <= 0 ? 99m : Math.Round(StockActual / StockMinimo, 2);

    /// <summary>Valor en dinero de lo que hay en bodega.</summary>
    [NotMapped]
    public decimal ValorEnBodega => StockActual * CostoUnitario;
}
