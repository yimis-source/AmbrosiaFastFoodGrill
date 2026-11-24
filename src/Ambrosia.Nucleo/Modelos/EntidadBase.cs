namespace Ambrosia.Nucleo.Modelos;

/// <summary>
/// Clase base ABSTRACTA de todas las entidades que se guardan en la base de datos.
/// No se puede instanciar por sí sola: solo sirve para heredar la clave primaria "Id".
/// </summary>
public abstract class EntidadBase
{
    /// <summary>Clave primaria (EF Core la reconoce por convención).</summary>
    public int Id { get; set; }

    /// <summary>Indica si la entidad todavía no se ha guardado.</summary>
    public bool EsNueva => Id == 0;
}
