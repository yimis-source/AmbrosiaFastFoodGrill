// Entidad RecetaItem: cantidad de un insumo que usa un producto (une Producto e Insumo).
namespace Ambrosia.Nucleo.Modelos;

public class RecetaItem : EntidadBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public int InsumoId { get; set; }
    public Insumo? Insumo { get; set; }

    public decimal Cantidad { get; set; }
}
