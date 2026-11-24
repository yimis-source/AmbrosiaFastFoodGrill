using Ambrosia.Escritorio.Formularios;
using Ambrosia.Nucleo.Datos;

namespace Ambrosia.Escritorio;

internal static class Program
{
    /// <summary>Punto de entrada de la aplicación de escritorio.</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 1) Crear la base de datos compartida (la misma que usa la web) si aún no existe.
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            InicializadorBaseDatos.InicializarAsync(db).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo abrir la base de datos:\n\n" + ex.Message,
                "Ambrosía", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // 2) Ciclo de sesión: login → ventana principal → (cerrar sesión) → login…
        while (true)
        {
            using var login = new FormLogin();
            if (login.ShowDialog() != DialogResult.OK || login.Sesion is null)
                return;

            using var principal = new FormPrincipal(login.Sesion);
            principal.ShowDialog();
            if (!principal.CerrarSesionSolicitado)
                return;
        }
    }
}
