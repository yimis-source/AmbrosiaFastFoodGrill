using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Nucleo.Utilidades;

namespace Ambrosia.Escritorio.Controles;

/// <summary>Consulta de pedidos del día, con entrega y cancelación.</summary>
public class ControlPedidos : UserControl, IRecargable
{
    private readonly DateTimePicker _fecha = new() { Format = DateTimePickerFormat.Short, Width = 130, Font = Tema.Normal };
    private readonly ComboBox _cmbEstado = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Font = Tema.Normal };
    private readonly DataGridView _grid = Tema.Grilla();
    private readonly Label _lblTotal = new() { AutoSize = true, Font = Tema.Titulo, Margin = new Padding(12, 6, 3, 3) };

    public event EventHandler<string>? Mensaje;

    public ControlPedidos()
    {
        BackColor = Tema.Crema;

        // "Todos" + cada estado. Se usa object porque la lista mezcla un texto con valores del enum.
        _cmbEstado.Items.Add("Todos");
        foreach (var estado in Extensiones.Valores<EstadoPedido>())
            _cmbEstado.Items.Add(estado);
        _cmbEstado.FormattingEnabled = true;
        _cmbEstado.Format += (_, e) => { if (e.ListItem is EstadoPedido estado) e.Value = estado.Texto(); };
        _cmbEstado.SelectedIndex = 0;

        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 6) };
        var btnBuscar = Tema.Boton("🔍 Buscar", Tema.Carbon, Color.White);
        btnBuscar.Click += async (_, _) => await CargarAsync();
        barra.Controls.AddRange(new Control[] { Tema.Etiqueta("Fecha"), _fecha, Tema.Etiqueta("Estado"), _cmbEstado, btnBuscar, _lblTotal });

        Tema.Columna(_grid, "codigo", "Código", 10);
        Tema.Columna(_grid, "hora", "Hora", 8);
        Tema.Columna(_grid, "destino", "Destino", 30);
        Tema.Columna(_grid, "productos", "Productos", 30);
        Tema.Columna(_grid, "pago", "Pago", 10);
        Tema.Columna(_grid, "total", "Total", 12, derecha: true);
        Tema.Columna(_grid, "estado", "Estado", 12);

        var acciones = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        var btnEntregar = Tema.Boton("🛎️ Entregar (solo listos)", Tema.Verde, Color.White);
        var btnCancelar = Tema.Boton("Cancelar (solo pendientes)", Color.White, Color.Firebrick);
        btnEntregar.Click += async (_, _) => await EjecutarAsync(EstadoPedido.Entregado);
        btnCancelar.Click += async (_, _) => await EjecutarAsync(EstadoPedido.Cancelado);
        acciones.Controls.AddRange(new Control[] { btnEntregar, btnCancelar });

        Controls.Add(_grid);
        Controls.Add(acciones);
        Controls.Add(barra);

        Load += async (_, _) => await CargarAsync();
    }

    public async Task CargarAsync()
    {
        try
        {
            EstadoPedido? estado = _cmbEstado.SelectedItem is EstadoPedido e ? e : null;
            var dia = DateOnly.FromDateTime(_fecha.Value);

            using var db = ConfiguracionBaseDatos.CrearContexto();
            var pedidos = await new ServicioPedidos(db, new ServicioInventario(db)).ListarAsync(estado, dia);

            _grid.Rows.Clear();
            foreach (var p in pedidos)
            {
                var indice = _grid.Rows.Add(
                    p.Codigo,
                    p.FechaCreacion.ToString("HH:mm"),
                    p.Destino,
                    string.Join(", ", p.Detalles.Select(d => $"{d.Cantidad}× {d.NombreProducto}")),
                    p.MetodoPago.Texto(),
                    p.Total.ComoPesos(),
                    p.Estado.Texto());
                var fila = _grid.Rows[indice];
                fila.Tag = p.Id;
                fila.DefaultCellStyle.BackColor = ColorEstado(p.Estado);
            }

            var vendido = pedidos.Where(p => p.Estado != EstadoPedido.Cancelado).Sum(p => p.Total);
            _lblTotal.Text = $"{pedidos.Count} pedido(s) · {vendido.ComoPesos()} vendidos";
        }
        catch (Exception ex)
        {
            Tema.MostrarError("No se pudieron cargar los pedidos: " + ex.Message);
        }
    }

    /// <summary>Color de la fila según el estado (switch de expresiones).</summary>
    private static Color ColorEstado(EstadoPedido estado) => estado switch
    {
        EstadoPedido.Pendiente => Tema.AmarilloSuave,
        EstadoPedido.EnPreparacion => Tema.AzulSuave,
        EstadoPedido.Listo => Tema.VerdeSuave,
        EstadoPedido.Cancelado => Tema.RojoSuave,
        _ => Color.White
    };

    private async Task EjecutarAsync(EstadoPedido nuevoEstado)
    {
        if (_grid.CurrentRow?.Tag is not int id)
        {
            Tema.MostrarError("Seleccione un pedido de la lista.");
            return;
        }
        if (nuevoEstado == EstadoPedido.Cancelado &&
            MessageBox.Show("¿Cancelar el pedido? Los insumos vuelven al inventario.", "Ambrosía",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            var pedido = await new ServicioPedidos(db, new ServicioInventario(db)).CambiarEstadoAsync(id, nuevoEstado);
            Mensaje?.Invoke(this, $"Pedido {pedido.Codigo}: {pedido.Estado.Texto()}.");
        }
        catch (ReglaNegocioException ex)
        {
            Tema.MostrarError(ex.Message);
        }
        await CargarAsync();
    }
}
