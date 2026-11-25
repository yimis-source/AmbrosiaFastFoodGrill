// Ventana principal: muestra solo las pestañas permitidas para el rol del usuario.
using Ambrosia.Escritorio.Controles;
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Seguridad;

namespace Ambrosia.Escritorio.Formularios;

public class FormPrincipal : Form
{
    private readonly SesionUsuario _sesion;
    private readonly TabControl _pestanas = new() { Dock = DockStyle.Fill, Font = Tema.Normal, Padding = new Point(14, 6) };
    private readonly ToolStripStatusLabel _lblEstado = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

    public bool CerrarSesionSolicitado { get; private set; }

    public FormPrincipal(SesionUsuario sesion)
    {
        _sesion = sesion;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        var rol = Roles.Todos.FirstOrDefault(sesion.TieneRol) ?? "Sin rol";

        Text = $"Ambrosía Fast Food & Grill — {sesion.NombreUsuario} ({rol})";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 650);
        BackColor = Tema.Crema;
        Font = Tema.Normal;
        KeyPreview = true;

        var encabezado = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Tema.Carbon, Padding = new Padding(12, 0, 12, 0) };
        var titulo = new Label
        {
            Text = "🔥 AMBROSÍA  ·  Fast Food & Grill",
            Font = Tema.Titulo,
            ForeColor = Color.White,
            AutoSize = true,
            UseMnemonic = false,
            Location = new Point(12, 14)
        };
        var usuario = new Label
        {
            Text = $"👤 {sesion.NombreUsuario}  ·  {rol}",
            ForeColor = Tema.Mostaza,
            Font = Tema.Negrita,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        var btnSalir = Tema.Boton("Cerrar sesión", Tema.Carbon, Color.White);
        btnSalir.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSalir.Click += (_, _) => { CerrarSesionSolicitado = true; Close(); };

        var derecha = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, Padding = new Padding(0, 10, 0, 0), WrapContents = false };
        usuario.Margin = new Padding(3, 9, 12, 3);
        derecha.Controls.Add(usuario);
        derecha.Controls.Add(btnSalir);
        encabezado.Controls.Add(derecha);
        encabezado.Controls.Add(titulo);

        var barraEstado = new StatusStrip();
        barraEstado.Items.Add(_lblEstado);
        MostrarEstado($"Bienvenido, {sesion.NombreUsuario}. Base de datos: {ConfiguracionBaseDatos.RutaPorDefecto}");

        var esAdmin = sesion.TieneRol(Roles.Administrador);
        var esCajero = esAdmin || sesion.TieneRol(Roles.Cajero);
        var esCocina = esAdmin || sesion.TieneRol(Roles.Cocina);

        ControlPedidos? controlPedidos = null;
        if (esCajero)
        {
            var caja = new ControlCaja(sesion.NombreUsuario);
            caja.Mensaje += (_, texto) => MostrarEstado(texto);
            AgregarPestana("🧾 Caja", caja);

            controlPedidos = new ControlPedidos();
            controlPedidos.Mensaje += (_, texto) => MostrarEstado(texto);
            AgregarPestana("📋 Pedidos", controlPedidos);

            caja.PedidoRegistrado += async (_, _) => await controlPedidos.CargarAsync();
        }
        if (esCocina)
        {
            var cocina = new ControlCocina();
            cocina.Mensaje += (_, texto) => MostrarEstado(texto);
            AgregarPestana("👨‍🍳 Cocina", cocina);
        }
        if (esAdmin)
        {
            var inventario = new ControlInventario();
            inventario.Mensaje += (_, texto) => MostrarEstado(texto);
            AgregarPestana("📦 Inventario", inventario);
        }

        _pestanas.SelectedIndexChanged += async (_, _) =>
        {
            if (_pestanas.SelectedTab is { } pestana && pestana.Controls.Count > 0 && pestana.Controls[0] is IRecargable recargable)
                await recargable.CargarAsync();
        };

        Controls.Add(_pestanas);
        Controls.Add(encabezado);
        Controls.Add(barraEstado);
    }

    private void AgregarPestana(string titulo, UserControl control)
    {
        var pestana = new TabPage(titulo) { BackColor = Tema.Crema, Padding = new Padding(8) };
        control.Dock = DockStyle.Fill;
        pestana.Controls.Add(control);
        _pestanas.TabPages.Add(pestana);
    }

    private void MostrarEstado(string texto) =>
        _lblEstado.Text = $"{DateTime.Now:HH:mm}  {texto}";
}
