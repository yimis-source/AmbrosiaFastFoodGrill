namespace Ambrosia.Escritorio;

/// <summary>
/// Colores, fuentes y "fábricas" de controles con el estilo de Ambrosía.
/// Centralizarlo evita repetir las mismas propiedades en cada pantalla.
/// </summary>
internal static class Tema
{
    public static readonly Color Carbon = Color.FromArgb(31, 26, 23);
    public static readonly Color Fuego = Color.FromArgb(232, 89, 12);
    public static readonly Color Mostaza = Color.FromArgb(244, 180, 0);
    public static readonly Color Crema = Color.FromArgb(255, 248, 239);
    public static readonly Color Verde = Color.FromArgb(25, 135, 84);
    public static readonly Color RojoSuave = Color.FromArgb(248, 215, 218);
    public static readonly Color AmarilloSuave = Color.FromArgb(255, 243, 205);
    public static readonly Color AzulSuave = Color.FromArgb(207, 244, 252);
    public static readonly Color VerdeSuave = Color.FromArgb(209, 231, 221);
    public static readonly Color GrisSuave = Color.FromArgb(233, 236, 239);

    public static readonly Font Normal = new("Segoe UI", 10f);
    public static readonly Font Negrita = new("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font Titulo = new("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font Total = new("Segoe UI", 18f, FontStyle.Bold);

    public static Button Boton(string texto, Color? fondo = null, Color? letra = null)
    {
        var boton = new Button
        {
            Text = texto,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = fondo ?? Color.White,
            ForeColor = letra ?? Carbon,
            Font = Normal,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(3),
            Cursor = Cursors.Hand
        };
        boton.FlatAppearance.BorderColor = fondo ?? Color.Silver;
        return boton;
    }

    public static Button BotonPrincipal(string texto) => Boton(texto, Fuego, Color.White);

    public static Label Etiqueta(string texto, Font? fuente = null) => new()
    {
        Text = texto,
        AutoSize = true,
        Font = fuente ?? Normal,
        Margin = new Padding(3, 8, 3, 2)
    };

    /// <summary>DataGridView de solo lectura, selección por fila y columnas que llenan el ancho.</summary>
    public static DataGridView Grilla()
    {
        var grilla = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = Normal,
            EnableHeadersVisualStyles = false
        };
        grilla.ColumnHeadersDefaultCellStyle.BackColor = Carbon;
        grilla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grilla.ColumnHeadersDefaultCellStyle.Font = Negrita;
        grilla.ColumnHeadersHeight = 32;
        grilla.RowTemplate.Height = 28;
        grilla.DefaultCellStyle.SelectionBackColor = Fuego;
        grilla.DefaultCellStyle.SelectionForeColor = Color.White;
        return grilla;
    }

    /// <summary>Agrega una columna de texto a una grilla.</summary>
    public static void Columna(DataGridView grilla, string nombre, string titulo, float peso = 100, bool derecha = false)
    {
        var columna = new DataGridViewTextBoxColumn
        {
            Name = nombre,
            HeaderText = titulo,
            FillWeight = peso,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        if (derecha)
            columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grilla.Columns.Add(columna);
    }

    public static void MostrarError(string mensaje) =>
        MessageBox.Show(mensaje, "Ambrosía", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void MostrarInfo(string mensaje) =>
        MessageBox.Show(mensaje, "Ambrosía", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
