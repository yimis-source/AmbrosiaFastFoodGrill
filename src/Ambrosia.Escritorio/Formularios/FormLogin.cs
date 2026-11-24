using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Servicios;

namespace Ambrosia.Escritorio.Formularios;

/// <summary>
/// Inicio de sesión con los MISMOS usuarios de ASP.NET Core Identity que usa la web
/// (las contraseñas se verifican con el PasswordHasher de Identity).
/// </summary>
public class FormLogin : Form
{
    private readonly TextBox _txtUsuario = new() { Width = 260, Font = Tema.Normal };
    private readonly TextBox _txtClave = new() { Width = 260, Font = Tema.Normal, UseSystemPasswordChar = true };
    private readonly Label _lblError = new() { AutoSize = true, ForeColor = Color.Firebrick, Font = Tema.Normal, Margin = new Padding(3, 6, 3, 3) };
    private readonly Button _btnEntrar = Tema.BotonPrincipal("Entrar");

    /// <summary>Sesión obtenida al iniciar correctamente (null si se canceló).</summary>
    public SesionUsuario? Sesion { get; private set; }

    public FormLogin()
    {
        Text = "Ambrosía · Iniciar sesión";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Tema.Crema;
        Font = Tema.Normal;

        var encabezado = new Label
        {
            Text = "🔥 AMBROSÍA\nFast Food & Grill",
            Font = Tema.Titulo,
            ForeColor = Color.White,
            BackColor = Tema.Carbon,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 70
        };

        var contenido = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            AutoSize = true,
            Padding = new Padding(24, 12, 24, 16),
            Dock = DockStyle.Top,
            WrapContents = false
        };
        contenido.Controls.Add(Tema.Etiqueta("Usuario"));
        contenido.Controls.Add(_txtUsuario);
        contenido.Controls.Add(Tema.Etiqueta("Contraseña"));
        contenido.Controls.Add(_txtClave);
        contenido.Controls.Add(_lblError);
        _btnEntrar.Width = 260;
        _btnEntrar.AutoSize = false;
        _btnEntrar.Height = 38;
        contenido.Controls.Add(_btnEntrar);
        contenido.Controls.Add(new Label
        {
            Text = "Prueba: admin / Admin123*  ·  cajero / Cajero123*  ·  cocina / Cocina123*",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.5f),
            MaximumSize = new Size(270, 0),
            Margin = new Padding(3, 10, 3, 3)
        });

        // El orden de agregado importa con Dock: primero el contenido y luego el encabezado arriba.
        Controls.Add(contenido);
        Controls.Add(encabezado);

        AcceptButton = _btnEntrar;   // Enter = Entrar
        _btnEntrar.Click += BtnEntrar_Click;
    }

    private async void BtnEntrar_Click(object? sender, EventArgs e)
    {
        _btnEntrar.Enabled = false;
        _lblError.Text = string.Empty;
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            var servicio = new ServicioAutenticacion(db);
            Sesion = await servicio.IniciarSesionAsync(_txtUsuario.Text, _txtClave.Text);

            if (Sesion is null)
            {
                _lblError.Text = "Usuario o contraseña incorrectos.";
                _txtClave.SelectAll();
                _txtClave.Focus();
                return;
            }
            if (Sesion.Roles.Count == 0)
            {
                _lblError.Text = "El usuario no tiene un rol asignado.";
                Sesion = null;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _lblError.Text = "Error: " + ex.Message;
        }
        finally
        {
            _btnEntrar.Enabled = true;
        }
    }
}
