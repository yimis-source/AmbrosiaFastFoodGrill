// Punto de entrada del escritorio: crea la base de datos y alterna login y ventana principal.
using Ambrosia.Escritorio.Formularios;
using Ambrosia.Nucleo.Datos;

namespace Ambrosia.Escritorio;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

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
