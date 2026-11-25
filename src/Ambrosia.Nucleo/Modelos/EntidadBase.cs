// Clase base abstracta con el Id de todas las entidades.
namespace Ambrosia.Nucleo.Modelos;

public abstract class EntidadBase
{
    public int Id { get; set; }

    public bool EsNueva => Id == 0;
}
