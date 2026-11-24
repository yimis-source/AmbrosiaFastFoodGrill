namespace Ambrosia.Escritorio.Controles;

/// <summary>Pantallas que saben volver a leer sus datos de la base de datos.</summary>
public interface IRecargable
{
    Task CargarAsync();
}
