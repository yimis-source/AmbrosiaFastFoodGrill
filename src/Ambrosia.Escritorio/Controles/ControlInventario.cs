// Inventario del escritorio: existencias, compras y alertas de stock bajo.
using Ambrosia.Nucleo.Datos;
using Ambrosia.Nucleo.Excepciones;
using Ambrosia.Nucleo.Modelos;
using Ambrosia.Nucleo.Servicios;
using Ambrosia.Nucleo.Utilidades;

namespace Ambrosia.Escritorio.Controles;

public class ControlInventario : UserControl, IRecargable
{
    private readonly DataGridView _grid = Tema.Grilla();
    private readonly ListBox _lstAlertas = new() { Dock = DockStyle.Fill, Font = Tema.Normal, IntegralHeight = false };
    private readonly NumericUpDown _numCantidad = new() { DecimalPlaces = 2, Minimum = 0.01m, Maximum = 1_000_000m, Value = 1000m, Width = 120, Font = Tema.Normal, ThousandsSeparator = true };
    private readonly Label _lblSeleccion = new() { AutoSize = true, Font = Tema.Negrita, Margin = new Padding(3, 8, 3, 3) };
    private readonly Label _lblValor = new() { AutoSize = true, Font = Tema.Titulo, Margin = new Padding(12, 6, 3, 3) };

    public event EventHandler<string>? Mensaje;

    public ControlInventario()
    {
        BackColor = Tema.Crema;

        Tema.Columna(_grid, "insumo", "Insumo", 34);
        Tema.Columna(_grid, "stock", "Stock", 16, derecha: true);
        Tema.Columna(_grid, "minimo", "Mínimo", 16, derecha: true);
        Tema.Columna(_grid, "nivel", "Nivel", 10, derecha: true);
        Tema.Columna(_grid, "costo", "Costo", 12, derecha: true);
        Tema.Columna(_grid, "valor", "Valor en bodega", 14, derecha: true);
        _grid.SelectionChanged += (_, _) =>
            _lblSeleccion.Text = _grid.CurrentRow is { } fila ? $"Insumo: {fila.Cells[0].Value}" : "Seleccione un insumo";

        var compra = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, Padding = new Padding(0, 6, 0, 0) };
        var btnIngresar = Tema.BotonPrincipal("📥 Ingresar compra");
        btnIngresar.Click += BtnIngresar_Click;
        compra.Controls.AddRange(new Control[] { _lblSeleccion, Tema.Etiqueta("Cantidad"), _numCantidad, btnIngresar });

        var izquierda = new Panel { Dock = DockStyle.Fill };
        izquierda.Controls.Add(_grid);
        izquierda.Controls.Add(compra);

        var alertas = new GroupBox { Text = "⚠ Comprar pronto (más urgente primero)", Dock = DockStyle.Right, Width = 340, Font = Tema.Normal, Padding = new Padding(6) };
        alertas.Controls.Add(_lstAlertas);

        var barra = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        var btnActualizar = Tema.Boton("⟳ Actualizar");
        btnActualizar.Click += async (_, _) => await CargarAsync();
        barra.Controls.AddRange(new Control[] { btnActualizar, _lblValor });

        Controls.Add(izquierda);
        Controls.Add(alertas);
        Controls.Add(barra);

        Load += async (_, _) => await CargarAsync();
    }

    public async Task CargarAsync()
    {
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            var servicio = new ServicioInventario(db);
            var insumos = await servicio.ListarAsync();
            var urgentes = await servicio.ObtenerAlertasAsync();

            var idSeleccionado = _grid.CurrentRow?.Tag as int?;
            _grid.Rows.Clear();
            foreach (var i in insumos)
            {
                var indice = _grid.Rows.Add(
                    i.Nombre,
                    i.StockActual.ComoCantidad(i.Unidad),
                    i.StockMinimo.ComoCantidad(i.Unidad),
                    $"{(i.NivelStock * 100).ComoNumero(0)} %",
                    $"{i.CostoUnitario.ComoPesos()}/{i.Unidad.Abreviatura()}",
                    i.ValorEnBodega.ComoPesos());
                var fila = _grid.Rows[indice];
                fila.Tag = i.Id;
                if (i.BajoMinimo) fila.DefaultCellStyle.BackColor = Tema.RojoSuave;
                if (i.Id == idSeleccionado) _grid.CurrentCell = fila.Cells[0];
            }

            _lstAlertas.Items.Clear();
            var posicion = 1;
            foreach (var u in urgentes)
                _lstAlertas.Items.Add($"{posicion++}. {u.Nombre}: {u.StockActual.ComoCantidad(u.Unidad)} (mín. {u.StockMinimo.ComoCantidad(u.Unidad)})");
            if (urgentes.Count == 0)
                _lstAlertas.Items.Add("Todo está sobre el mínimo ✔");

            _lblValor.Text = $"Valor del inventario: {insumos.Sum(i => i.ValorEnBodega).ComoPesos()}";
        }
        catch (Exception ex)
        {
            Tema.MostrarError("No se pudo cargar el inventario: " + ex.Message);
        }
    }

    private async void BtnIngresar_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow?.Tag is not int id)
        {
            Tema.MostrarError("Seleccione un insumo de la tabla.");
            return;
        }
        try
        {
            using var db = ConfiguracionBaseDatos.CrearContexto();
            var insumo = await new ServicioInventario(db).ReabastecerAsync(id, _numCantidad.Value);
            Mensaje?.Invoke(this, $"Ingresaron {_numCantidad.Value.ComoCantidad(insumo.Unidad)} de {insumo.Nombre}. " +
                                  $"Nuevo stock: {insumo.StockActual.ComoCantidad(insumo.Unidad)}.");
        }
        catch (ReglaNegocioException ex)
        {
            Tema.MostrarError(ex.Message);
        }
        await CargarAsync();
    }
}
