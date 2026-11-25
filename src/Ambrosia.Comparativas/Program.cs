// Mide con Stopwatch cada estructura de datos elegida frente a su alternativa.
using System.Diagnostics;
using Ambrosia.Nucleo.Modelos;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("COMPARATIVAS DE ESTRUCTURAS DE DATOS · Ambrosía Fast Food & Grill");
Console.WriteLine($".NET {Environment.Version} · {Environment.ProcessorCount} núcleos · {DateTime.Now:yyyy-MM-dd HH:mm}");
Console.WriteLine(new string('=', 92));

var resultados = new List<Resultado>();
var azar = new Random(2026);

{
    const int productos = 10_000, busquedas = 50_000;
    var lista = Enumerable.Range(1, productos).Select(i => new Producto { Id = i, Nombre = $"P{i}", Precio = 1000 + i }).ToList();
    var diccionario = lista.ToDictionary(p => p.Id);
    var ids = Enumerable.Range(0, busquedas).Select(_ => azar.Next(1, productos + 1)).ToArray();

    resultados.Add(Comparar(
        "Buscar producto por Id",
        $"{productos:N0} productos, {busquedas:N0} búsquedas",
        "List<Producto>.Find  O(n)", () => { decimal s = 0; foreach (var id in ids) s += lista.Find(p => p.Id == id)!.Precio; return s; },
        "Dictionary<int,Producto>  O(1)", () => { decimal s = 0; foreach (var id in ids) s += diccionario[id].Precio; return s; }));
}

{
    const int pedidos = 50_000;
    resultados.Add(Comparar(
        "Sacar el primer pedido de la cola",
        $"{pedidos:N0} pedidos atendidos",
        "List.RemoveAt(0)  O(n)", () =>
        {
            var lista = Enumerable.Range(1, pedidos).ToList();
            long s = 0;
            while (lista.Count > 0) { s += lista[0]; lista.RemoveAt(0); }
            return s;
        },
        "Queue.Dequeue  O(1)", () =>
        {
            var cola = new Queue<int>(Enumerable.Range(1, pedidos));
            long s = 0;
            while (cola.TryDequeue(out var p)) s += p;
            return s;
        }));
}

{
    const int agotados = 5_000, consultas = 200_000;
    var listaAgotados = Enumerable.Range(0, agotados).Select(i => i * 2).ToList();
    var conjunto = listaAgotados.ToHashSet();
    var preguntas = Enumerable.Range(0, consultas).Select(_ => azar.Next(0, agotados * 4)).ToArray();

    resultados.Add(Comparar(
        "¿El producto está agotado?",
        $"{agotados:N0} agotados, {consultas:N0} consultas",
        "List<int>.Contains  O(n)", () => preguntas.Count(listaAgotados.Contains),
        "HashSet<int>.Contains  O(1)", () => preguntas.Count(conjunto.Contains)));
}

{
    const int lineas = 2_000, insumosPorReceta = 8, insumos = 500;
    var recetas = Enumerable.Range(0, lineas)
        .Select(_ => Enumerable.Range(0, insumosPorReceta).Select(_ => (Insumo: azar.Next(insumos), Cantidad: (decimal)azar.Next(1, 300))).ToArray())
        .ToArray();

    resultados.Add(Comparar(
        "Acumular insumos requeridos",
        $"{lineas:N0} líneas × {insumosPorReceta} ingredientes",
        "List<(id,cant)> + búsqueda  O(n·m)", () =>
        {
            var acumulado = new List<(int Id, decimal Cantidad)>();
            foreach (var receta in recetas)
                foreach (var (id, cantidad) in receta)
                {
                    var i = acumulado.FindIndex(x => x.Id == id);
                    if (i < 0) acumulado.Add((id, cantidad));
                    else acumulado[i] = (id, acumulado[i].Cantidad + cantidad);
                }
            return acumulado.Count;
        },
        "Dictionary<int,decimal> acumulador", () =>
        {
            var acumulado = new Dictionary<int, decimal>();
            foreach (var receta in recetas)
                foreach (var (id, cantidad) in receta)
                    acumulado[id] = acumulado.GetValueOrDefault(id) + cantidad;
            return acumulado.Count;
        }));
}

{
    const int insumos = 200_000, k = 3;
    var niveles = Enumerable.Range(0, insumos).Select(i => (Id: i, Nivel: (decimal)azar.NextDouble())).ToArray();

    resultados.Add(Comparar(
        "Top 3 insumos más críticos",
        $"{insumos:N0} insumos, se piden {k}",
        "OrderBy(...).Take(3)  O(n log n)", () => niveles.OrderBy(x => x.Nivel).Take(k).Sum(x => x.Id),
        "PriorityQueue (montículo)", () =>
        {
            var cola = new PriorityQueue<int, decimal>(niveles.Select(x => (x.Id, x.Nivel)));
            var s = 0;
            for (var i = 0; i < k && cola.TryDequeue(out var id, out _); i++) s += id;
            return s;
        }));
}

{
    const int dias = 365, ventas = 50_000, lecturas = 500;
    var fechas = Enumerable.Range(0, ventas).Select(_ => new DateOnly(2026, 1, 1).AddDays(azar.Next(dias))).ToArray();

    resultados.Add(Comparar(
        "Ventas por día en orden",
        $"{ventas:N0} ventas, {dias} días, {lecturas} lecturas",
        "Dictionary + OrderBy en cada lectura", () =>
        {
            var porDia = new Dictionary<DateOnly, int>();
            foreach (var f in fechas) porDia[f] = porDia.GetValueOrDefault(f) + 1;
            long s = 0;
            for (var i = 0; i < lecturas; i++) s += porDia.OrderBy(x => x.Key).First().Value;
            return s;
        },
        "SortedDictionary (siempre ordenado)", () =>
        {
            var porDia = new SortedDictionary<DateOnly, int>();
            foreach (var f in fechas) porDia[f] = porDia.GetValueOrDefault(f) + 1;
            long s = 0;
            for (var i = 0; i < lecturas; i++) s += porDia.First().Value;
            return s;
        }));
}

{
    const int acciones = 1_000_000;
    resultados.Add(Comparar(
        "Deshacer la última acción",
        $"{acciones:N0} acciones apiladas y deshechas",
        "List.RemoveAt(Count-1)", () =>
        {
            var lista = new List<int>();
            for (var i = 0; i < acciones; i++) lista.Add(i);
            long s = 0;
            while (lista.Count > 0) { s += lista[^1]; lista.RemoveAt(lista.Count - 1); }
            return s;
        },
        "Stack.Push / Pop", () =>
        {
            var pila = new Stack<int>();
            for (var i = 0; i < acciones; i++) pila.Push(i);
            long s = 0;
            while (pila.TryPop(out var x)) s += x;
            return s;
        }));
}

Console.WriteLine();
Console.WriteLine($"{"Escenario",-34} {"Alternativa",12} {"Elegida",12} {"Veces más rápida",18}");
Console.WriteLine(new string('-', 80));
foreach (var r in resultados)
    Console.WriteLine($"{r.Escenario,-34} {r.MsAlternativa,9:N2} ms {r.MsElegida,9:N2} ms {r.Factor,15:N1} ×");
Console.WriteLine();
Console.WriteLine("Notas: mediana de 5 corridas después de un calentamiento. Los tiempos cambian según el equipo;");
Console.WriteLine("lo importante es la diferencia de orden de magnitud. En «Deshacer» ambas son O(1): Stack se");
Console.WriteLine("eligió por expresar la intención (LIFO) y no permitir acceso al medio de la pila por error.");

static Resultado Comparar(string escenario, string tamano, string nombreA, Func<object> alternativa, string nombreB, Func<object> elegida)
{
    var a = Medir(alternativa, out var resA);
    var b = Medir(elegida, out var resB);
    if (!Equals(resA.ToString(), resB.ToString()))
        throw new InvalidOperationException($"Los resultados no coinciden en «{escenario}»: {resA} vs {resB}");

    Console.WriteLine($"\n▶ {escenario}  ({tamano})");
    Console.WriteLine($"   {nombreA,-40} {a,10:N2} ms");
    Console.WriteLine($"   {nombreB,-40} {b,10:N2} ms   ← elegida");
    return new Resultado(escenario, a, b);
}

static double Medir(Func<object> accion, out object resultado)
{
    resultado = accion();
    var tiempos = new List<double>();
    for (var i = 0; i < 5; i++)
    {
        var reloj = Stopwatch.StartNew();
        resultado = accion();
        reloj.Stop();
        tiempos.Add(reloj.Elapsed.TotalMilliseconds);
    }
    tiempos.Sort();
    return tiempos[2];
}

internal record Resultado(string Escenario, double MsAlternativa, double MsElegida)
{
    public double Factor => MsElegida <= 0 ? 0 : MsAlternativa / MsElegida;
}
