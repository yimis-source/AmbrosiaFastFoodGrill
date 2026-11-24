# Estructuras de datos: justificación y comparativas

Cada estructura se eligió por **la operación que más se repite** en ese punto del negocio. Para cada una se muestra dónde está, la alternativa que se descartó, la complejidad de ambas y una medición real hecha con el proyecto `Ambrosia.Comparativas`.

> Cómo reproducir las mediciones: `dotnet run -c Release --project src/Ambrosia.Comparativas`
> Resultados de referencia: .NET 8, equipo de prueba de 2 núcleos, mediana de 5 corridas. En otro computador los milisegundos cambian, pero las diferencias de orden de magnitud se mantienen.

## Resumen

| # | Estructura elegida | Dónde | Operación clave | Alternativa descartada | Alternativa | Elegida | Resultado |
|---|---|---|---|---|---:|---:|---|
| 1 | `Dictionary<int, Producto>` | Carrito, creación de pedidos | Buscar producto por Id | `List.Find` | 453,84 ms | 0,62 ms | **730× más rápida** |
| 2 | `Queue<Pedido>` | Cola de cocina | Sacar el pedido más antiguo | `List.RemoveAt(0)` | 108,32 ms | 0,20 ms | **550× más rápida** |
| 3 | `HashSet<int>` | Productos agotados | ¿Está agotado? | `List<int>.Contains` | 36,16 ms | 8,42 ms | **4× más rápida** |
| 4 | `Dictionary<int, decimal>` | Insumos requeridos | Acumular por insumo | `List` + búsqueda | 9,99 ms | 1,69 ms | **6× más rápida** |
| 5 | `SortedDictionary<DateOnly, …>` | Reporte por día | Leer en orden de fecha | `Dictionary` + `OrderBy` | 17,43 ms | 11,00 ms | **1,6× más rápida** |
| 6 | `PriorityQueue<Insumo, decimal>` | Alertas de compra | Los más urgentes primero | `OrderBy().Take()` | 9,97 ms | 14,09 ms | 0,7× (más lenta) |
| 7 | `Stack<…>` | Deshacer en la caja | Revertir la última acción | `List.RemoveAt(Count-1)` | 7,85 ms | 9,57 ms | 0,8× (más lenta) |
| — | `List<DetallePedido>` | Líneas del pedido | Recorrer en orden | Arreglo `[]` | — | — | Tamaño variable |
| — | `record` / `record struct` | DTO, rangos de fechas | Copiar y comparar valores | Clases mutables | — | — | Inmutabilidad |

**Conclusión:** en 5 de 7 casos la estructura elegida es la más rápida. En los casos 6 y 7 la velocidad empata o es menor, y la decisión se tomó por **claridad** (la estructura expresa exactamente la regla del negocio). Eso también se explica abajo, porque elegir una estructura no es solo elegir la más rápida.

---

## 1. `Dictionary<int, Producto>` — búsqueda por Id

**Dónde:** `ServicioPedidos.CrearPedidoAsync` (`ToDictionaryAsync(p => p.Id)`), `ControlCaja._productos` (escritorio) y `CarritoPedido` (líneas por Id de producto).

**Por qué:** al armar un pedido se consulta el producto de cada línea varias veces: para validar que esté disponible, leer su precio y su receta. Con la clave Id, cada consulta cuesta **O(1)**.

```csharp
// Descartado: recorre la lista hasta encontrarlo → O(n) por búsqueda
var producto = productos.Find(p => p.Id == id);

// Elegido: acceso directo por clave → O(1)
var productos = await _db.Productos.Include(p => p.Receta)
    .Where(p => ids.Contains(p.Id))
    .ToDictionaryAsync(p => p.Id);
var producto = productos[id];
```

**Medición:** 10.000 productos y 50.000 búsquedas: **453,84 ms** con lista frente a **0,62 ms** con diccionario.

**Uso extra en el carrito:** si el cajero marca dos veces la "Ambrosía Clásica", el diccionario encuentra la línea existente y suma la cantidad, en vez de crear dos líneas repetidas.

---

## 2. `Queue<Pedido>` — cola FIFO de cocina

**Dónde:** `Estructuras/ColaCocina.cs`, usada por `ServicioPedidos.TomarSiguienteAsync`, la vista web de cocina y `ControlCocina` (escritorio).

**Por qué:** la regla justa de una cocina es *primero en llegar, primero en salir*. `Queue` modela esa regla y `Dequeue` cuesta **O(1)**. Con `List` habría que hacer `RemoveAt(0)`, que desplaza todos los elementos una posición: **O(n)** por pedido atendido.

```csharp
// Descartado
var siguiente = lista[0];
lista.RemoveAt(0);                 // mueve todos los demás elementos

// Elegido
_cola = new Queue<Pedido>(pendientes.OrderBy(p => p.FechaCreacion).ThenBy(p => p.Id));
public Pedido? TomarSiguiente() => _cola.Count > 0 ? _cola.Dequeue() : null;   // FIFO
```

**Medición:** atender 50.000 pedidos: **108,32 ms** con lista frente a **0,20 ms** con cola.

**Alternativa considerada:** `PriorityQueue` para priorizar domicilios. Se descartó porque en horas pico los pedidos de mesa podrían quedarse esperando indefinidamente detrás de domicilios nuevos.

---

## 3. `HashSet<int>` — productos agotados

**Dónde:** `ServicioInventario.ObtenerProductosAgotadosAsync`; lo usan la carta pública, el formulario de nuevo pedido y la caja de escritorio.

**Por qué:** al dibujar la carta se pregunta, producto por producto, `agotados.Contains(producto.Id)`. En un `HashSet` esa pregunta es **O(1)** y además no admite repetidos (un producto con dos insumos faltantes aparece una sola vez).

```csharp
// Descartado: cada Contains recorre la lista → O(n), y puede tener Id repetidos
List<int> agotados = recetas.Where(...).Select(r => r.ProductoId).ToList();

// Elegido
HashSet<int> agotados = recetas.Where(r => r.Insumo!.StockActual < r.Cantidad)
                               .Select(r => r.ProductoId)
                               .ToHashSet();
```

**Medición:** 5.000 agotados y 200.000 consultas: **36,16 ms** frente a **8,42 ms**. La diferencia es menor que en los casos anteriores porque `List<int>.Contains` en .NET usa instrucciones vectorizadas, pero crece a medida que crece la lista.

También se usa `HashSet<string>` para los roles del usuario en el escritorio (`SesionUsuario.TieneRol`) y `HashSet<int>` al generar los pedidos de ejemplo (para no repetir productos).

---

## 4. `Dictionary<int, decimal>` como acumulador de insumos

**Dónde:** `ServicioInventario.CalcularRequerimientos`.

**Por qué:** varios productos comparten insumos (pan, queso, salsa BBQ de ajo, aceite). Para saber cuánto se necesita de cada uno, se suma por clave en **O(1)**:

```csharp
foreach (var (producto, cantidad) in lineas)
    foreach (var item in producto.Receta)
        requerimientos[item.InsumoId] =
            requerimientos.GetValueOrDefault(item.InsumoId) + item.Cantidad * cantidad;
```

Con una lista de tuplas habría que buscar el insumo antes de cada suma (`FindIndex`), lo que da **O(n·m)**.

**Medición:** 2.000 líneas con 8 ingredientes cada una: **9,99 ms** frente a **1,69 ms**.

---

## 5. `SortedDictionary<DateOnly, VentaDiaria>` — reporte por día

**Dónde:** `ServicioReportes.VentasPorDia` y `ReporteVentas.VentasPorDia`.

**Por qué:** el reporte siempre se muestra en orden cronológico. `SortedDictionary` (árbol binario balanceado) mantiene las claves ordenadas al insertar (**O(log n)**), así que leerlo en orden no requiere ordenar cada vez. Además se precargan todos los días del rango en cero, para que los días sin ventas también aparezcan.

```csharp
// Descartado: Dictionary no garantiza orden → hay que ordenar en cada lectura
foreach (var (dia, venta) in porDia.OrderBy(x => x.Key)) { ... }

// Elegido: el orden lo garantiza la estructura
var porDia = new SortedDictionary<DateOnly, VentaDiaria>();
foreach (var (dia, venta) in porDia) { ... }
```

**Medición:** 50.000 ventas en 365 días con 500 lecturas: **17,43 ms** frente a **11,00 ms**. Con los 7 a 31 días de un reporte normal la diferencia es mínima; el beneficio principal es que el orden queda **garantizado** por la estructura.

---

## 6. `PriorityQueue<Insumo, decimal>` — alertas de compra

**Dónde:** `ServicioInventario.ObtenerAlertasAsync`; se ve en Inventario (web y escritorio) y en "Insumos más críticos" del reporte.

**Por qué:** la prioridad es el nivel `stock / mínimo` (0,75 = al 75 % del mínimo). El montículo entrega siempre el insumo más urgente con `Dequeue`, que es exactamente la idea de una lista de compras por urgencia.

```csharp
var cola = new PriorityQueue<Insumo, decimal>(
    insumos.Where(i => i.BajoMinimo).Select(i => (i, i.NivelStock)));   // se arma en O(n)
while (resultado.Count < maximo && cola.TryDequeue(out var urgente, out _))
    resultado.Add(urgente);                                               // O(log n) cada uno
```

**Medición (resultado honesto):** con 200.000 insumos para obtener los 3 más críticos, `OrderBy(...).Take(3)` tardó **9,97 ms** y `PriorityQueue` **14,09 ms**. LINQ en .NET 8 detecta el `Take` después del `OrderBy` y hace un ordenamiento parcial interno; además comparar `decimal` es costoso.

**Decisión final:** se mantiene `PriorityQueue` porque el restaurante maneja unas 30 materias primas (la diferencia es de microsegundos) y la estructura comunica la intención: «dame el siguiente insumo más urgente». Si el inventario creciera a cientos de miles de referencias, se cambiaría por `OrderBy().Take()`.

---

## 7. `Stack<…>` — botón Deshacer de la caja

**Dónde:** `Estructuras/CarritoPedido.cs` (`_historial`), usado por el botón y el atajo **Ctrl+Z** de `ControlCaja`.

**Por qué:** deshacer es **LIFO**: la última acción es la primera que se revierte. Cada acción guarda cómo estaba la línea antes del cambio (o `null` si no existía), y `Pop` la restaura.

```csharp
private readonly Stack<(int ProductoId, LineaCarrito? Anterior)> _historial = new();

public bool Deshacer()
{
    if (_historial.Count == 0) return false;
    var accion = _historial.Pop();                 // LIFO: sale la última acción
    if (accion.Anterior is null) _lineas.Remove(accion.ProductoId);
    else _lineas[accion.ProductoId] = accion.Anterior;
    return true;
}
```

**Medición (resultado honesto):** apilar y deshacer 1.000.000 de acciones: `List.RemoveAt(Count-1)` **7,85 ms** frente a `Stack` **9,57 ms**. Las dos son **O(1)**; la diferencia es ruido de implementación.

**Decisión final:** `Stack` porque su interfaz (`Push`/`Pop`) solo permite trabajar sobre la cima. Con una `List` alguien podría borrar o leer una acción del medio por error y dañar el historial.

---

## Otras decisiones

| Estructura / tipo | Dónde | Por qué |
|---|---|---|
| `List<DetallePedido>`, `List<RecetaItem>` | Navegaciones de EF Core | Conservan el orden en que se digitaron, crecen según el pedido y EF Core las llena al consultar. Un arreglo tiene tamaño fijo. |
| `record LineaCarrito` + `with` | Carrito | Las líneas son inmutables: para cambiar la cantidad se crea una copia (`linea with { Cantidad = … }`). Por eso el historial puede guardar el estado anterior sin que se modifique después. |
| `readonly record struct RangoFechas` | Reportes | Dos fechas son un valor pequeño: como `struct` no genera basura en memoria, y el constructor valida que la fecha final no sea anterior a la inicial. |
| `Dictionary<TClave, decimal>` genérico | `ServicioReportes.AgruparTotales<TClave>` | Un solo método agrupa por método de pago, por tipo de pedido o por usuario: la clave la decide el delegado `Func<Pedido, TClave>`. |
| Enumeraciones (`enum`) | Estados, tipos, pagos, unidades | Limitan los valores posibles; en la base de datos se guardan como texto para que sea legible. |
