using OrdenamientosU5;

string destino = args.Length > 0 ? args[0] : "Resultados_U5.csv";
var resultados = BancoPruebas.Ejecutar(
    [10_000, 50_000, 100_000],
    Enum.GetValues<Condicion>(),
    progreso: new ProgresoConsola());

BancoPruebas.ExportarCsv(destino, resultados);
Console.WriteLine($"{resultados.Count} combinaciones verificadas. CSV: {Path.GetFullPath(destino)}");

sealed class ProgresoConsola : IProgress<string>
{
    public void Report(string mensaje) => Console.WriteLine(mensaje);
}
