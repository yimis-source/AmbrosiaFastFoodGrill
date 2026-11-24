using Ambrosia.Nucleo.Modelos;

namespace Ambrosia.Nucleo.Datos;

/// <summary>
/// Datos iniciales de Ambrosía Fast Food &amp; Grill: carta, insumos y recetas.
/// Los precios están en pesos colombianos.
/// </summary>
internal static class DatosSemilla
{
    internal record CategoriaSemilla(string Nombre, string Icono, int Orden);

    internal record InsumoSemilla(string Nombre, UnidadMedida Unidad, decimal Stock, decimal Minimo, decimal Costo);

    internal record ProductoSemilla(
        string Nombre, string Categoria, decimal Precio, int Minutos, string Descripcion,
        params (string Insumo, decimal Cantidad)[] Receta);

    internal static readonly CategoriaSemilla[] Categorias =
    {
        new("Hamburguesas", "🍔", 1),
        new("Perros calientes", "🌭", 2),
        new("Asados a la parrilla", "🥩", 3),
        new("Acompañantes", "🍟", 4),
        new("Bebidas", "🥤", 5),
    };

    // Algunos insumos arrancan cerca o por debajo del mínimo para que se vean las alertas.
    internal static readonly InsumoSemilla[] Insumos =
    {
        new("Pan brioche", UnidadMedida.Unidad, 60, 20, 1_200),
        new("Carne de ampolleta molida", UnidadMedida.Gramo, 9_000, 3_000, 32),
        new("Pechuga de pollo", UnidadMedida.Gramo, 7_000, 2_500, 19),
        new("Queso mozzarella", UnidadMedida.Gramo, 3_500, 1_000, 30),
        new("Tocineta", UnidadMedida.Gramo, 1_000, 800, 42),
        new("Lechuga", UnidadMedida.Gramo, 2_000, 500, 6),
        new("Tomate", UnidadMedida.Gramo, 3_000, 800, 5),
        new("Cebolla", UnidadMedida.Gramo, 4_000, 1_000, 4),
        new("Salsa BBQ de ajo (de la casa)", UnidadMedida.Mililitro, 2_500, 1_000, 14),
        new("Pan para perro", UnidadMedida.Unidad, 40, 15, 700),
        new("Salchicha americana", UnidadMedida.Unidad, 35, 15, 1_800),
        new("Salchicha ranchera", UnidadMedida.Unidad, 22, 10, 2_600),
        new("Papa ripio", UnidadMedida.Gramo, 1_800, 500, 16),
        new("Papa a la francesa precortada", UnidadMedida.Gramo, 12_000, 4_000, 7),
        new("Papa criolla", UnidadMedida.Gramo, 6_000, 2_000, 6),
        new("Churrasco de res", UnidadMedida.Gramo, 5_400, 1_800, 39),
        new("Costilla de cerdo", UnidadMedida.Gramo, 1_800, 2_000, 23),
        new("Chorizo santarrosano", UnidadMedida.Unidad, 30, 10, 2_600),
        new("Chicharrón", UnidadMedida.Gramo, 2_500, 1_000, 26),
        new("Arepa", UnidadMedida.Unidad, 40, 15, 600),
        new("Aros de cebolla congelados", UnidadMedida.Gramo, 3_000, 1_000, 15),
        new("Gaseosa 400 ml", UnidadMedida.Unidad, 48, 24, 2_300),
        new("Cerveza nacional", UnidadMedida.Unidad, 18, 24, 3_200),
        new("Limón", UnidadMedida.Unidad, 60, 20, 300),
        new("Leche de coco", UnidadMedida.Mililitro, 2_000, 800, 9),
        new("Pulpa de fruta", UnidadMedida.Gramo, 4_000, 1_000, 8),
        new("Aceite para freír", UnidadMedida.Mililitro, 12_000, 4_000, 9),
    };

    internal static readonly ProductoSemilla[] Productos =
    {
        // ---------- Hamburguesas ----------
        new("Ambrosía Clásica", "Hamburguesas", 22_000, 12,
            "150 g de carne de ampolleta de res, queso mozzarella, lechuga, tomate y nuestra salsa BBQ de ajo en pan brioche.",
            ("Pan brioche", 1), ("Carne de ampolleta molida", 150), ("Queso mozzarella", 30),
            ("Lechuga", 15), ("Tomate", 25), ("Salsa BBQ de ajo (de la casa)", 30)),
        new("Ambrosía Doble Tocineta", "Hamburguesas", 30_000, 14,
            "Doble carne de ampolleta (300 g), doble queso, tocineta crocante, cebolla y salsa BBQ de ajo.",
            ("Pan brioche", 1), ("Carne de ampolleta molida", 300), ("Queso mozzarella", 60),
            ("Tocineta", 40), ("Cebolla", 20), ("Salsa BBQ de ajo (de la casa)", 40)),
        new("Crispy de Pollo", "Hamburguesas", 20_000, 12,
            "Pechuga apanada crocante, queso, lechuga y salsa de la casa.",
            ("Pan brioche", 1), ("Pechuga de pollo", 160), ("Queso mozzarella", 30),
            ("Lechuga", 15), ("Salsa BBQ de ajo (de la casa)", 20), ("Aceite para freír", 40)),

        // ---------- Perros calientes ----------
        new("Perro Ambrosía", "Perros calientes", 15_000, 8,
            "Salchicha americana, papita ripio, queso gratinado y salsas.",
            ("Pan para perro", 1), ("Salchicha americana", 1), ("Papa ripio", 30),
            ("Queso mozzarella", 25), ("Salsa BBQ de ajo (de la casa)", 15)),
        new("Perro Ranchero", "Perros calientes", 18_000, 9,
            "Salchicha ranchera, tocineta, cebolla caramelizada y queso.",
            ("Pan para perro", 1), ("Salchicha ranchera", 1), ("Tocineta", 25),
            ("Cebolla", 30), ("Queso mozzarella", 25)),

        // ---------- Asados ----------
        new("Churrasco 300 g", "Asados a la parrilla", 38_000, 20,
            "Corte de res a la parrilla con papa criolla y ensalada fresca.",
            ("Churrasco de res", 300), ("Papa criolla", 150), ("Lechuga", 30), ("Tomate", 40)),
        new("Costillas BBQ", "Asados a la parrilla", 36_000, 22,
            "Costillas de cerdo glaseadas en salsa BBQ de ajo, con papas a la francesa.",
            ("Costilla de cerdo", 450), ("Salsa BBQ de ajo (de la casa)", 60),
            ("Papa a la francesa precortada", 150), ("Aceite para freír", 30)),
        new("Pechuga a la parrilla", "Asados a la parrilla", 28_000, 18,
            "Pechuga marinada con papa criolla y ensalada fresca.",
            ("Pechuga de pollo", 250), ("Papa criolla", 150), ("Lechuga", 30), ("Tomate", 40)),
        new("Picada Ambrosía (2 personas)", "Asados a la parrilla", 55_000, 25,
            "Carne, pollo, chorizo, chicharrón, papa criolla y arepa para compartir.",
            ("Churrasco de res", 150), ("Pechuga de pollo", 150), ("Chorizo santarrosano", 2),
            ("Chicharrón", 150), ("Papa criolla", 250), ("Arepa", 2)),

        // ---------- Acompañantes ----------
        new("Papas a la francesa", "Acompañantes", 7_000, 6,
            "Porción de papas crocantes.",
            ("Papa a la francesa precortada", 200), ("Aceite para freír", 40)),
        new("Papa criolla frita", "Acompañantes", 7_000, 7,
            "Papa criolla dorada con sal de la casa.",
            ("Papa criolla", 200), ("Aceite para freír", 40)),
        new("Aros de cebolla", "Acompañantes", 9_000, 6,
            "Aros apanados, ideales para acompañar la BBQ de ajo.",
            ("Aros de cebolla congelados", 150), ("Aceite para freír", 40)),

        // ---------- Bebidas ----------
        new("Gaseosa 400 ml", "Bebidas", 4_500, 1, "Bebida gaseosa personal.", ("Gaseosa 400 ml", 1)),
        new("Limonada de coco", "Bebidas", 9_000, 4, "Limonada cremosa con leche de coco.",
            ("Limón", 2), ("Leche de coco", 120)),
        new("Jugo natural en agua", "Bebidas", 6_500, 4, "Fruta de temporada.", ("Pulpa de fruta", 150)),
        new("Cerveza nacional", "Bebidas", 6_000, 1, "Botella 330 ml.", ("Cerveza nacional", 1)),
    };

    internal static readonly string[] Clientes =
    {
        "Laura Gómez", "Andrés Rivas", "Valentina Ospina", "Camilo Mosquera",
        "Daniela Caicedo", "Santiago Lozano", "Juliana Arboleda", "Mateo Valencia"
    };

    internal static readonly string[] Direcciones =
    {
        "Cra 66 # 9-35, El Limonar", "Calle 5 # 38-20, San Fernando", "Av. Roosevelt # 30-12",
        "Calle 13 # 85-40, Ciudad Jardín", "Cra 100 # 16-30, Valle del Lili", "Calle 9 # 44-18, El Templete"
    };
}
