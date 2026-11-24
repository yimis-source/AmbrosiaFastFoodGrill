namespace Ambrosia.Nucleo.Modelos;

/// <summary>
/// Una línea de la receta de un producto: "la Ambrosía Clásica usa 150 g de carne de ampolleta".
/// Es la tabla intermedia de la relación muchos a muchos entre Producto e Insumo.
/// </summary>
public class RecetaItem : EntidadBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    /// <summary>Cantidad del insumo (en su unidad de medida) para UNA unidad del producto.</summary>
    public decimal Cantidad { get; set; }
}
