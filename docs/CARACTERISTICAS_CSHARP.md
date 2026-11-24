# Características de C# y .NET usadas en el proyecto

Mapa para la sustentación: cada característica con el archivo donde se puede mostrar.

## Lenguaje C#

| Característica | Ejemplo en el proyecto | Archivo |
|---|---|---|
| Clases, propiedades, constructores | `Producto`, `Insumo`, `Pedido` | `Nucleo/Modelos/*.cs` |
| **Clase abstracta** y herencia | `EntidadBase` (Id) heredada por todas las entidades | `Nucleo/Modelos/EntidadBase.cs` |
| **Abstracción con interfaces** | `ICalculadorCargo` (contrato que usa el servicio de pedidos, como `ISensor` en clase) | `Nucleo/Servicios/Cargos.cs` |
| **Polimorfismo con `virtual` / `override`** | Clase abstracta `CalculadorCargo` (`virtual Descripcion`, `abstract Calcular`) y subclases `SinCargo`, `CargoServicioMesa`, `CargoDomicilio` que redefinen con `override`; prueba con `List<CalculadorCargo>` recorrida con `foreach` | `Nucleo/Servicios/Cargos.cs`, `Pruebas/ReglasTests.cs` |
| Patrón fábrica | `FabricaCargos.Para(tipo)` | `Nucleo/Servicios/Cargos.cs` |
| `sealed` | Implementaciones de cargos, `ItemPedido` | `Cargos.cs`, `ControlCocina.cs` |
| **Enumeraciones** | `EstadoPedido`, `TipoPedido`, `MetodoPago`, `UnidadMedida` | `Nucleo/Modelos/Enumeraciones.cs` |
| **Records** e inicializadores `init` | `SolicitudPedido`, `ItemSolicitado`, `VentaDiaria`, `LineaCarrito` | `Nucleo/Modelos/Registros.cs`, `CarritoPedido.cs` |
| Expresión `with` (copia de records) | `actual with { Cantidad = actual.Cantidad + cantidad }` | `Nucleo/Estructuras/CarritoPedido.cs` |
| **Struct** (`readonly record struct`) | `RangoFechas` con validación en el constructor | `Nucleo/Modelos/Registros.cs` |
| **Genéricos** con restricciones | `AgruparTotales<TClave>(…) where TClave : notnull`, `Valores<T>() where T : struct, Enum` | `ServicioReportes.cs`, `Extensiones.cs` |
| **Delegados** (`Func<…>`) y lambdas | Selector de clave en reportes; `EjecutarAsync(id, Func<Task<Pedido>>, Func<Pedido,string>)` | `ServicioReportes.cs`, `PedidosController.cs` |
| **Eventos** (`event EventHandler<T>`) | `ServicioInventario.StockBajo`, `CarritoPedido.Cambiado`, `ControlCaja.PedidoRegistrado` | `ServicioInventario.cs`, `CarritoPedido.cs`, `ControlCaja.cs` |
| **Excepciones propias** (jerarquía) | `ReglaNegocioException` → `StockInsuficienteException`, `TransicionInvalidaException`, `EntidadNoEncontradaException` | `Nucleo/Excepciones/ReglaNegocioException.cs` |
| `try / catch / finally` | Registro de pedidos en escritorio y web | `ControlCaja.cs`, `PedidosController.cs` |
| **async / await** y `Task` | Todos los servicios y controladores | `Nucleo/Servicios/*.cs` |
| **LINQ** (métodos) | `GroupBy`, `SelectMany`, `OrderByDescending`, `Take`, `Sum` en el ranking | `ServicioReportes.MasVendidos` |
| **LINQ** (sintaxis de consulta + `join`) | Roles del usuario | `ServicioAutenticacion.ObtenerRolesAsync` |
| **Pattern matching** | `switch` sobre tuplas (máquina de estados), patrones relacionales `is null or < 1 or > 30`, `case … when` | `ServicioPedidos.cs` |
| Expresiones `switch` | Colores por estado, cargos, abreviaturas | `Presentacion.cs`, `Cargos.cs`, `Extensiones.cs` |
| **Métodos de extensión** y sobrecarga | `precio.ComoPesos()`, `dia.DiaCorto()` y `Texto()` sobrecargado para cada enumeración | `Nucleo/Utilidades/Extensiones.cs` |
| **Indexador** | `carrito[idProducto]` | `CarritoPedido.cs` |
| Tuplas y deconstrucción | `foreach (var (insumoId, cantidad) in requerimientos)` | `ServicioInventario.cs` |
| Tipos que aceptan null (`?`, `?.`, `??`, `!`) | Proyecto con `<Nullable>enable</Nullable>` | `Directory.Build.props` |
| Clases y miembros estáticos, constantes | `Roles`, `ConfiguracionBaseDatos`, `Tema` | `Seguridad/Roles.cs`, `Datos/ConfiguracionBaseDatos.cs` |
| Modificadores de acceso (`internal`, `InternalsVisibleTo`) | `ServicioInventario.Descontar` visible solo para la biblioteca y sus pruebas | `ServicioInventario.cs`, `Ambrosia.Nucleo.csproj` |
| `params` y parámetros opcionales/nombrados | `ProductoSemilla(… params (string, decimal)[] Receta)`, `ObtenerAlertasAsync(maximo: 3)` | `DatosSemilla.cs`, `ReportesController.cs` |
| Instrucciones de nivel superior | Programa de comparativas | `Ambrosia.Comparativas/Program.cs` |
| Interpolación de cadenas y literales numéricos con `_` | `$"Pedido {pedido.Codigo}"`, `80_000m` | Todo el proyecto |

## Framework .NET

| Componente | Uso | Archivo |
|---|---|---|
| Colecciones genéricas | `List`, `Dictionary`, `HashSet`, `Queue`, `Stack`, `SortedDictionary`, `PriorityQueue` | Ver `docs/ESTRUCTURAS_DE_DATOS.md` |
| **Entity Framework Core** | `DbContext`, `DbSet`, API fluida (`HasOne/WithMany`, `HasIndex().IsUnique()`, `HasConversion<string>()`, `OnDelete`), `Include/ThenInclude`, transacción implícita de `SaveChanges` | `Nucleo/Datos/AmbrosiaDbContext.cs` |
| SQLite | Base de datos en archivo, compartida por web y escritorio | `ConfiguracionBaseDatos.cs` |
| `IDesignTimeDbContextFactory` | Soporte para `dotnet ef migrations` | `ConfiguracionBaseDatos.cs` |
| **ASP.NET Core MVC** | Controladores, vistas Razor, modelos de vista, *tag helpers*, validación con *data annotations*, `TempData` (Post-Redirect-Get), vistas parciales, secciones | `Ambrosia.Web/` |
| Inyección de dependencias | `AddScoped<ServicioPedidos>()`, constructores de controladores | `Ambrosia.Web/Program.cs` |
| **ASP.NET Core Identity** | `SignInManager`, `UserManager`, `IdentityDbContext`, cookies, bloqueo por intentos, `[Authorize(Roles = …)]`, `PasswordHasher` | `CuentaController.cs`, `UsuariosController.cs`, `ServicioAutenticacion.cs` |
| Seguridad web | `[ValidateAntiForgeryToken]`, `[Bind]` contra *overposting*, `Url.IsLocalUrl` | Controladores |
| **Windows Forms** | `Form`, `UserControl`, `DataGridView`, `TabControl`, `TableLayoutPanel`, `Timer`, `StatusStrip`, atajos de teclado (`ProcessCmdKey`) | `Ambrosia.Escritorio/` |
| **xUnit** | `[Fact]`, `[Theory]` + `[InlineData]`, pruebas asíncronas con SQLite real | `tests/Ambrosia.Pruebas/` |
| `Stopwatch` | Mediciones de las comparativas | `Ambrosia.Comparativas/Program.cs` |
