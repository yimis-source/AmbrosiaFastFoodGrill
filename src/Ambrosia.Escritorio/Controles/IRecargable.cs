// Contrato de las pantallas que pueden volver a leer sus datos.
namespace Ambrosia.Escritorio.Controles;

public interface IRecargable
{
    Task CargarAsync();
}
