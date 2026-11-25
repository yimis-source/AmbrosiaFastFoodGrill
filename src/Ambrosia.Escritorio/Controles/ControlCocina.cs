// Cocina del escritorio: cola FIFO, pedidos en preparación y listos; se refresca sola.
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Estructuras;
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Nucleo.Utilidades;

namespace Ambrosia.Escritorio.Controles;

public class ControlCocina : UserControl, IRecargable
{
    private sealed record ItemPedido(Pedido Pedido, string Texto)
    {
        public override string ToString() => Texto;
    }

    private readonly ListBox _lstCola = CrearLista();
    private readonly ListBox _lstPreparacion = CrearLista();
    private readonly ListBox _lstListos = CrearLista();
    private readonly TextBox _txtDetalle = new()
    {
        Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical,
        Font = new Font("Segoe UI", 11f), BackColor = Color.White
    };
    private readonly Label _lblResumen = new() { AutoSize = true, Font = Tema.Normal, Margin = new Padding(12, 12, 3, 3) };
    private readonly Button _btnTomar = Tema.BotonPrincipal("🔥 Tomar siguiente");
    private readonly Button _btnListo = Tema.Boton("✅ Marcar listo", Tema.Verde, Color.White);
    private readonly System.Windows.Forms.Timer _temporizador = new() { Interval = 15_000 };

    public event EventHandler<string>? Mensaje;

    public ControlCocina()
    {
        BackColor = Tema.Crema;
        ConstruirInterfaz();
        Load += async (_, _) => { await CargarAsync(); _temporizador.Start(); };
        _temporizador.Tick += async (_, _) => await CargarAsync();
    }

    private static ListBox CrearLista() => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 11f),
        IntegralHeight = false,
        BorderStyle = BorderStyle.FixedSingle
    };

    private void ConstruirInterfaz()
    {
        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 6) };
        _btnTomar.Font = Tema.Titulo;
        _btnTomar.Click += BtnTomar_Click;
        var btnActualizar = Tema.Boton("⟳ Actualizar");
        btnActualizar.Click += async (_, _) => await CargarAsync();
        barra.Controls.AddRange(new Control[] { _btnTomar, btnActualizar, _lblResumen });

        var columnas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };
        for (var i = 0; i < 4; i++)
            columnas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        columnas.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        columnas.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        columnas.Controls.Add(Encabezado("🕐 En cola (orden de llegada)", Tema.Mostaza, Tema.Carbon), 0, 0);
        columnas.Controls.Add(Encabezado("🔥 En preparación", Color.FromArgb(13, 202, 240), Tema.Carbon), 1, 0);
        columnas.Controls.Add(Encabezado("✅ Listos para entregar", Tema.Verde, Color.White), 2, 0);
        columnas.Controls.Add(Encabezado("📝 Detalle del pedido", Tema.Carbon, Color.White), 3, 0);

        var panelPreparacion = new Panel { Dock = DockStyle.Fill };
        _btnListo.Dock = DockStyle.Bottom;
        _btnListo.AutoSize = false;
        _btnListo.Height = 44;
        _btnListo.Font = Tema.Titulo;
        _btnListo.Click += BtnListo_Click;
        panelPreparacion.Controls.Add(_lstPreparacion);
        panelPreparacion.Controls.Add(_btnListo);

        columnas.Controls.Add(_lstCola, 0, 1);
        columnas.Controls.Add(panelPreparacion, 1, 1);
        columnas.Controls.Add(_lstListos, 2, 1);
        columnas.Controls.Add(_txtDetalle, 3, 1);

        foreach (var lista in new[] { _lstCola, _lstPreparacion, _lstListos })
            lista.SelectedIndexChanged += (_, _) => MostrarDetalle(lista.SelectedItem as ItemPedido);

        Controls.Add(columnas);
        Controls.Add(barra);
    }

    private static Label Encabezado(string texto, Color fondo, Color letra) => new()
    {
        Text = texto,
        Dock = DockStyle.Fill,
        Height = 34,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = Tema.Negrita,
        BackColor = fondo,
        ForeColor = letra,
        Padding = new Padding(6, 0, 0, 0),
        Margin = new Padding(3, 3, 3, 0)
    };

    public async Task CargarAsync()
    {
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            var servicio = new ServicioPedidos(db, new ServicioInventario(db));
            ColaCocina cola = await servicio.ObtenerColaCocinaAsync();
            var enPreparacion = await servicio.ListarPorEstadoAsync(EstadoPedido.EnPreparacion);
            var listos = await servicio.ListarPorEstadoAsync(EstadoPedido.Listo);
            var ahora = DateTime.Now;

            Llenar(_lstCola, cola.EnOrden.Select(p => new ItemPedido(p,
                $"#{cola.PosicionDe(p.Id)}  {p.Codigo} · {p.Destino} · {p.MinutosDesdeCreacion(ahora)} min")));
            Llenar(_lstPreparacion, enPreparacion.Select(p => new ItemPedido(p,
                $"{p.Codigo} · {p.Destino} · {p.MinutosDesdeCreacion(ahora)} min")));
            Llenar(_lstListos, listos.Select(p => new ItemPedido(p, $"{p.Codigo} · {p.Destino}")));

            _btnTomar.Enabled = !cola.EstaVacia;
            _btnTomar.Text = cola.Siguiente is { } siguiente ? $"🔥 Tomar siguiente ({siguiente.Codigo})" : "🔥 Tomar siguiente";
            _lblResumen.Text = $"{cola.Cantidad} en cola · ~{cola.MinutosParaVaciar} min de trabajo · actualizado {ahora:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _lblResumen.Text = "No se pudo actualizar: " + ex.Message;
        }
    }

    private static void Llenar(ListBox lista, IEnumerable<ItemPedido> items)
    {
        var idSeleccionado = (lista.SelectedItem as ItemPedido)?.Pedido.Id;
        lista.BeginUpdate();
        lista.Items.Clear();
        foreach (var item in items)
        {
            var indice = lista.Items.Add(item);
            if (item.Pedido.Id == idSeleccionado)
                lista.SelectedIndex = indice;
        }
        lista.EndUpdate();
    }

    private void MostrarDetalle(ItemPedido? item)
    {
        if (item is null) return;
        var p = item.Pedido;
        var lineas = new List<string>
        {
            $"{p.Codigo}  ({p.Estado.Texto()})",
            p.Destino,
            $"Registrado {p.FechaCreacion:HH:mm} · estimado {p.MinutosEstimados} min",
            ""
        };
        foreach (var d in p.Detalles)
        {
            lineas.Add($"{d.Cantidad} × {d.NombreProducto}");
            if (!string.IsNullOrWhiteSpace(d.Nota))
                lineas.Add($"     📝 {d.Nota}");
        }
        if (!string.IsNullOrWhiteSpace(p.Observaciones))
            lineas.Add("\n🗒️ " + p.Observaciones);
        _txtDetalle.Text = string.Join(Environment.NewLine, lineas);
    }

    private async void BtnTomar_Click(object? sender, EventArgs e)
    {
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            var pedido = await new ServicioPedidos(db, new ServicioInventario(db)).TomarSiguienteAsync();
            Mensaje?.Invoke(this, pedido is null ? "No hay pedidos en cola." : $"Cocina preparando {pedido.Codigo}.");
        }
        catch (Exception ex)
        {
            Tema.MostrarError(ex.Message);
        }
        await CargarAsync();
    }

    private async void BtnListo_Click(object? sender, EventArgs e)
    {
        if (_lstPreparacion.SelectedItem is not ItemPedido item)
        {
            Tema.MostrarError("Seleccione un pedido de la columna «En preparación».");
            return;
        }
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            var pedido = await new ServicioPedidos(db, new ServicioInventario(db)).MarcarListoAsync(item.Pedido.Id);
            Mensaje?.Invoke(this, $"{pedido.Codigo} listo para entregar.");
        }
        catch (ReglaNegocioException ex)
        {
            Tema.MostrarError(ex.Message);
        }
        await CargarAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _temporizador.Dispose();
        base.Dispose(disposing);
    }
}
