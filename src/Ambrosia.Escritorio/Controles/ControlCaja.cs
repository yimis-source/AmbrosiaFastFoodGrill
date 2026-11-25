// Caja del escritorio: arma el pedido con CarritoPedido (deshacer con Ctrl+Z) y lo registra.
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Estructuras;
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Nucleo.Utilidades;

namespace Ambrosia.Escritorio.Controles;

public class ControlCaja : UserControl, IRecargable
{
    private readonly string _usuario;
    private readonly CarritoPedido _carrito = new();

    private Dictionary<int, Producto> _productos = new();
    private List<GrupoMenu> _carta = new();
    private HashSet<int> _agotados = new();

    private readonly FlowLayoutPanel _categorias = new() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 4) };
    private readonly DataGridView _gridProductos = Tema.Grilla();
    private readonly DataGridView _gridCarrito = Tema.Grilla();
    private readonly NumericUpDown _numCantidad = new() { Minimum = 1, Maximum = 50, Value = 1, Width = 60, Font = Tema.Normal };
    private readonly TextBox _txtNota = new() { Width = 260, Font = Tema.Normal };

    private readonly ComboBox _cmbTipo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Font = Tema.Normal, FormattingEnabled = true };
    private readonly ComboBox _cmbPago = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Font = Tema.Normal, FormattingEnabled = true };
    private readonly NumericUpDown _numMesa = new() { Minimum = 1, Maximum = 30, Value = 1, Width = 70, Font = Tema.Normal };
    private readonly TextBox _txtCliente = new() { Width = 220, Font = Tema.Normal };
    private readonly TextBox _txtDireccion = new() { Width = 220, Font = Tema.Normal };
    private readonly TextBox _txtTelefono = new() { Width = 140, Font = Tema.Normal };
    private readonly TextBox _txtObservaciones = new() { Width = 220, Font = Tema.Normal };

    private readonly Label _lblSubtotal = new() { AutoSize = true, Font = Tema.Normal };
    private readonly Label _lblCargo = new() { AutoSize = true, Font = Tema.Normal };
    private readonly Label _lblTotal = new() { AutoSize = true, Font = Tema.Total, ForeColor = Tema.Fuego };
    private readonly Button _btnDeshacer = Tema.Boton("↶ Deshacer (Ctrl+Z)");
    private readonly Button _btnRegistrar = Tema.BotonPrincipal("✔ Registrar pedido");

    private int? _categoriaFiltro;

    public event EventHandler<string>? Mensaje;

    public event EventHandler<Pedido>? PedidoRegistrado;

    public ControlCaja(string usuario)
    {
        _usuario = usuario;
        BackColor = Tema.Crema;
        ConstruirInterfaz();

        _carrito.Cambiado += (_, _) => RefrescarCarrito();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var division = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        division.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        var izquierda = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        Tema.Columna(_gridProductos, "producto", "Producto", 55);
        Tema.Columna(_gridProductos, "categoria", "Categoría", 25);
        Tema.Columna(_gridProductos, "precio", "Precio", 20, derecha: true);
        _gridProductos.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) AgregarSeleccionado(); };

        var barraAgregar = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        var btnAgregar = Tema.BotonPrincipal("➕ Agregar");
        btnAgregar.Click += (_, _) => AgregarSeleccionado();
        barraAgregar.Controls.Add(Tema.Etiqueta("Cantidad"));
        barraAgregar.Controls.Add(_numCantidad);
        barraAgregar.Controls.Add(Tema.Etiqueta("Nota"));
        barraAgregar.Controls.Add(_txtNota);
        barraAgregar.Controls.Add(btnAgregar);

        izquierda.Controls.Add(_gridProductos);
        izquierda.Controls.Add(barraAgregar);
        izquierda.Controls.Add(_categorias);
        izquierda.Controls.Add(new Label { Text = "Carta (doble clic para agregar)", Font = Tema.Titulo, Dock = DockStyle.Top, Height = 30 });

        var derecha = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
        derecha.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        derecha.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        derecha.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        derecha.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        derecha.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Tema.Columna(_gridCarrito, "producto", "Producto", 45);
        Tema.Columna(_gridCarrito, "cantidad", "Cant.", 12, derecha: true);
        Tema.Columna(_gridCarrito, "subtotal", "Subtotal", 20, derecha: true);
        Tema.Columna(_gridCarrito, "nota", "Nota", 23);

        var botonesCarrito = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        var btnMenos = Tema.Boton("− Uno");
        var btnQuitar = Tema.Boton("✕ Quitar línea");
        var btnVaciar = Tema.Boton("Vaciar");
        btnMenos.Click += (_, _) => { if (IdCarritoSeleccionado() is int id) _carrito.QuitarUno(id); };
        btnQuitar.Click += (_, _) => { if (IdCarritoSeleccionado() is int id) _carrito.Quitar(id); };
        btnVaciar.Click += (_, _) => _carrito.Vaciar();
        _btnDeshacer.Click += (_, _) => _carrito.Deshacer();
        botonesCarrito.Controls.AddRange(new Control[] { btnMenos, btnQuitar, _btnDeshacer, btnVaciar });

        derecha.Controls.Add(new Label { Text = "Pedido actual", Font = Tema.Titulo, AutoSize = true }, 0, 0);
        derecha.Controls.Add(_gridCarrito, 0, 1);
        derecha.Controls.Add(botonesCarrito, 0, 2);
        derecha.Controls.Add(ConstruirDatosPedido(), 0, 3);
        derecha.Controls.Add(ConstruirTotales(), 0, 4);

        division.Controls.Add(izquierda, 0, 0);
        division.Controls.Add(derecha, 1, 0);
        Controls.Add(division);
    }

    private GroupBox ConstruirDatosPedido()
    {
        _cmbTipo.DataSource = Extensiones.Valores<TipoPedido>();
        _cmbTipo.Format += (_, e) => { if (e.ListItem is TipoPedido tipo) e.Value = tipo.Texto(); };
        _cmbPago.DataSource = Extensiones.Valores<MetodoPago>();
        _cmbPago.Format += (_, e) => { if (e.ListItem is MetodoPago pago) e.Value = pago.Texto(); };
        _cmbTipo.SelectedIndexChanged += (_, _) => { ActualizarCamposPorTipo(); RefrescarTotales(); };

        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, AutoSize = true };
        void Fila(string titulo1, Control control1, string? titulo2 = null, Control? control2 = null)
        {
            tabla.Controls.Add(Tema.Etiqueta(titulo1));
            tabla.Controls.Add(control1);
            if (titulo2 is not null && control2 is not null)
            {
                tabla.Controls.Add(Tema.Etiqueta(titulo2));
                tabla.Controls.Add(control2);
            }
            else
            {
                tabla.Controls.Add(new Label { AutoSize = true });
                tabla.Controls.Add(new Label { AutoSize = true });
            }
        }
        Fila("Tipo", _cmbTipo, "Mesa", _numMesa);
        Fila("Cliente", _txtCliente, "Pago", _cmbPago);
        Fila("Dirección", _txtDireccion, "Teléfono", _txtTelefono);
        Fila("Observ.", _txtObservaciones);

        var grupo = new GroupBox { Text = "Datos del pedido", Dock = DockStyle.Fill, AutoSize = true, Font = Tema.Normal, Padding = new Padding(6) };
        grupo.Controls.Add(tabla);
        return grupo;
    }

    private Control ConstruirTotales()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 6, 0, 0) };
        var columnaTextos = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        columnaTextos.Controls.Add(_lblSubtotal);
        columnaTextos.Controls.Add(_lblCargo);
        columnaTextos.Controls.Add(_lblTotal);
        _btnRegistrar.Font = Tema.Titulo;
        _btnRegistrar.Margin = new Padding(24, 12, 3, 3);
        _btnRegistrar.Click += BtnRegistrar_Click;
        panel.Controls.Add(columnaTextos);
        panel.Controls.Add(_btnRegistrar);
        return panel;
    }

    public async Task CargarAsync()
    {
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            _carta = await new ServicioCatalogo(db).ObtenerCartaAsync();
            _agotados = await new ServicioInventario(db).ObtenerProductosAgotadosAsync();
            _productos = _carta.SelectMany(g => g.Productos).ToDictionary(p => p.Id);
            ConstruirBotonesCategoria();
            MostrarProductos();
            ActualizarCamposPorTipo();
            RefrescarCarrito();
        }
        catch (Exception ex)
        {
            Tema.MostrarError("No se pudo cargar la carta: " + ex.Message);
        }
    }

    private void ConstruirBotonesCategoria()
    {
        _categorias.Controls.Clear();
        var todas = Tema.Boton("Todas", _categoriaFiltro is null ? Tema.Mostaza : Color.White);
        todas.Click += (_, _) => { _categoriaFiltro = null; ConstruirBotonesCategoria(); MostrarProductos(); };
        _categorias.Controls.Add(todas);
        foreach (var grupo in _carta)
        {
            var id = grupo.Categoria.Id;
            var boton = Tema.Boton($"{grupo.Categoria.Icono} {grupo.Categoria.Nombre}", _categoriaFiltro == id ? Tema.Mostaza : Color.White);
            boton.Click += (_, _) => { _categoriaFiltro = id; ConstruirBotonesCategoria(); MostrarProductos(); };
            _categorias.Controls.Add(boton);
        }
    }

    private void MostrarProductos()
    {
        _gridProductos.Rows.Clear();
        foreach (var grupo in _carta.Where(g => _categoriaFiltro is null || g.Categoria.Id == _categoriaFiltro))
        {
            foreach (var producto in grupo.Productos)
            {
                var agotado = _agotados.Contains(producto.Id);
                var indice = _gridProductos.Rows.Add(
                    agotado ? producto.Nombre + "  (agotado)" : producto.Nombre,
                    grupo.Categoria.Nombre,
                    producto.Precio.ComoPesos());
                var fila = _gridProductos.Rows[indice];
                fila.Tag = producto.Id;
                if (agotado) fila.DefaultCellStyle.ForeColor = Color.Gray;
            }
        }
    }

    private void AgregarSeleccionado()
    {
        if (_gridProductos.CurrentRow?.Tag is not int id || !_productos.TryGetValue(id, out var producto))
        {
            Mensaje?.Invoke(this, "Seleccione un producto de la carta.");
            return;
        }
        if (_agotados.Contains(id))
        {
            Tema.MostrarError($"«{producto.Nombre}» está agotado: no alcanza el inventario.");
            return;
        }
        _carrito.Agregar(producto, (int)_numCantidad.Value, _txtNota.Text);
        _numCantidad.Value = 1;
        _txtNota.Clear();
    }

    private int? IdCarritoSeleccionado() => _gridCarrito.CurrentRow?.Tag as int?;

    private void RefrescarCarrito()
    {
        _gridCarrito.Rows.Clear();
        foreach (var linea in _carrito.Lineas)
        {
            var indice = _gridCarrito.Rows.Add(linea.Nombre, linea.Cantidad, linea.Subtotal.ComoPesos(), linea.Nota ?? "");
            _gridCarrito.Rows[indice].Tag = linea.ProductoId;
        }
        _btnDeshacer.Enabled = _carrito.PuedeDeshacer;
        _btnDeshacer.Text = _carrito.PuedeDeshacer
            ? $"↶ Deshacer ({_carrito.AccionesEnHistorial})"
            : "↶ Deshacer";
        RefrescarTotales();
    }

    private TipoPedido TipoSeleccionado => _cmbTipo.SelectedItem is TipoPedido tipo ? tipo : TipoPedido.Mesa;

    private void RefrescarTotales()
    {
        ICalculadorCargo calculador = FabricaCargos.Para(TipoSeleccionado);
        var subtotal = _carrito.Subtotal;
        var cargo = _carrito.EstaVacio ? 0 : calculador.Calcular(subtotal);
        _lblSubtotal.Text = $"Subtotal: {subtotal.ComoPesos()}";
        _lblCargo.Text = $"{calculador.Descripcion}: {cargo.ComoPesos()}";
        _lblTotal.Text = $"Total {(subtotal + cargo).ComoPesos()}";
        _btnRegistrar.Enabled = !_carrito.EstaVacio;
    }

    private void ActualizarCamposPorTipo()
    {
        var tipo = TipoSeleccionado;
        _numMesa.Enabled = tipo == TipoPedido.Mesa;
        _txtCliente.Enabled = tipo != TipoPedido.Mesa;
        _txtDireccion.Enabled = tipo == TipoPedido.Domicilio;
        _txtTelefono.Enabled = tipo == TipoPedido.Domicilio;
    }

    private async void BtnRegistrar_Click(object? sender, EventArgs e)
    {
        var solicitud = new SolicitudPedido
        {
            Tipo = TipoSeleccionado,
            MetodoPago = _cmbPago.SelectedItem is MetodoPago pago ? pago : MetodoPago.Efectivo,
            Mesa = (int)_numMesa.Value,
            NombreCliente = _txtCliente.Enabled ? _txtCliente.Text : null,
            Direccion = _txtDireccion.Enabled ? _txtDireccion.Text : null,
            Telefono = _txtTelefono.Enabled ? _txtTelefono.Text : null,
            Observaciones = _txtObservaciones.Text,
            Items = _carrito.ComoItems()
        };

        _btnRegistrar.Enabled = false;
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            var inventario = new ServicioInventario(db);
            var avisos = new List<string>();
            inventario.StockBajo += (_, args) => avisos.Add(args.Mensaje);

            var pedido = await new ServicioPedidos(db, inventario).CrearPedidoAsync(solicitud, _usuario);

            _carrito.Vaciar();
            _txtCliente.Clear(); _txtDireccion.Clear(); _txtTelefono.Clear(); _txtObservaciones.Clear();
            Mensaje?.Invoke(this, $"Pedido {pedido.Codigo} registrado ({pedido.Total.ComoPesos()}).");
            PedidoRegistrado?.Invoke(this, pedido);

            var texto = $"Pedido {pedido.Codigo} enviado a cocina.\nTotal: {pedido.Total.ComoPesos()}";
            if (avisos.Count > 0)
                texto += "\n\n⚠ Inventario bajo:\n• " + string.Join("\n• ", avisos);
            Tema.MostrarInfo(texto);

            await CargarAsync();
        }
        catch (ReglaNegocioException ex)
        {
            Tema.MostrarError(ex.Message);
        }
        catch (Exception ex)
        {
            Tema.MostrarError("Error inesperado: " + ex.Message);
        }
        finally
        {
            RefrescarTotales();
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Z))
        {
            _carrito.Deshacer();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
}
