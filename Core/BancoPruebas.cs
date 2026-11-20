using System.Diagnostics;

namespace OrdenamientosU5;

public enum Condicion
{
    Aleatorio,
    Ordenado,
    Inverso
}

public sealed record Resultado(
    string Metodo, string Tipo, int Registros, Condicion Condicion,
    int Repeticiones, double TranscurridoMs, double CpuMs);

public static class BancoPruebas
{
    private static readonly string[] Metodos =
    [
        "Burbuja", "Quicksort", "Shellsort", "Radix",
        "Intercalación", "Mezcla directa", "Mezcla natural"
    ];

    public static IReadOnlyList<Resultado> Ejecutar(
        IReadOnlyList<int> tamanos, IReadOnlyList<Condicion> condiciones,
        int repeticiones = 3, IProgress<string>? progreso = null,
        CancellationToken cancelacion = default)
    {
        var resultados = new List<Resultado>();
        string carpeta = Path.Combine(Path.GetTempPath(), "OrdenamientosU5-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpeta);
        try
        {
            using var proceso = Process.GetCurrentProcess();
            Calentar(carpeta, proceso, cancelacion);

            foreach (int cantidad in tamanos)
            {
                var aleatorio = Generar(cantidad);
                var esperado = (int[])aleatorio.Clone();
                Array.Sort(esperado);
                var inverso = (int[])esperado.Clone();
                Array.Reverse(inverso);

                foreach (Condicion condicion in condiciones)
                {
                    int[] entrada = condicion switch
                    {
                        Condicion.Aleatorio => aleatorio,
                        Condicion.Ordenado => esperado,
                        Condicion.Inverso => inverso,
                        _ => throw new ArgumentOutOfRangeException(nameof(condiciones))
                    };

                    for (int metodo = 0; metodo < Metodos.Length; metodo++)
                    {
                        cancelacion.ThrowIfCancellationRequested();
                        progreso?.Report($"{Metodos[metodo]} · {cantidad:N0} registros · {condicion}");
                        var transcurridos = new double[repeticiones];
                        var tiemposCpu = new double[repeticiones];
                        for (int r = 0; r < repeticiones; r++)
                        {
                            var muestra = Medir(metodo, entrada, esperado, carpeta, proceso, cancelacion);
                            transcurridos[r] = muestra.Transcurrido;
                            tiemposCpu[r] = muestra.Cpu;
                        }

                        resultados.Add(new Resultado(
                            Metodos[metodo], metodo < 4 ? "Interno" : "Externo", cantidad, condicion,
                            repeticiones, Mediana(transcurridos), Mediana(tiemposCpu)));
                    }
                }
            }
            return resultados;
        }
        finally
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }

    private static int[] Generar(int cantidad)
    {
        var aleatorio = new Random(2026 + cantidad);
        var datos = new int[cantidad];
        for (int i = 0; i < cantidad; i++)
            datos[i] = aleatorio.Next(-1_000_000, 1_000_001);
        return datos;
    }

    private static void Calentar(string carpeta, Process proceso, CancellationToken cancelacion)
    {
        int[] datos = Generar(2048);
        datos[0] = int.MinValue;
        datos[1] = int.MaxValue;
        datos[2] = datos[3] = -12;
        int[] esperado = (int[])datos.Clone();
        Array.Sort(esperado);
        for (int metodo = 0; metodo < Metodos.Length; metodo++)
            Medir(metodo, datos, esperado, carpeta, proceso, cancelacion);
    }

    private static (double Transcurrido, double Cpu) Medir(
        int metodo, int[] entrada, int[] esperado, string carpeta,
        Process proceso, CancellationToken cancelacion)
    {
        cancelacion.ThrowIfCancellationRequested();
        string archivo = Path.Combine(carpeta, "entrada.bin");
        string salida = Path.Combine(carpeta, "salida.bin");
        int[]? copia = null;

        if (metodo < 4)
            copia = (int[])entrada.Clone();
        else
            EscribirArchivo(archivo, entrada);

        double cpuInicial = RelojCpu.Milisegundos(proceso);
        long inicio = Stopwatch.GetTimestamp();

        switch (metodo)
        {
            case 0: OrdenamientoInterno.Burbuja(copia!, cancelacion); break;
            case 1: OrdenamientoInterno.Quicksort(copia!); break;
            case 2: OrdenamientoInterno.Shellsort(copia!); break;
            case 3: OrdenamientoInterno.Radix(copia!); break;
            case 4: PrepararEIntercalar(archivo, salida, carpeta, entrada.Length); break;
            case 5: OrdenamientoExterno.MezclaDirecta(archivo, carpeta, entrada.Length); break;
            case 6: salida = OrdenamientoExterno.MezclaNatural(archivo, carpeta); break;
        }

        double transcurrido = Stopwatch.GetElapsedTime(inicio).TotalMilliseconds;
        double cpu = RelojCpu.Milisegundos(proceso) - cpuInicial;

        if (copia is not null)
        {
            if (!copia.AsSpan().SequenceEqual(esperado))
                throw new InvalidDataException($"{Metodos[metodo]} produjo un resultado incorrecto.");
        }
        else
        {
            ValidarArchivo(salida, esperado);
        }

        return (transcurrido, cpu);
    }

    private static void PrepararEIntercalar(string archivo, string salida, string carpeta, int cantidad)
    {
        string izquierda = Path.Combine(carpeta, "mitadA.bin");
        string derecha = Path.Combine(carpeta, "mitadB.bin");
        int mitad = (cantidad + 1) / 2;
        using (var lector = OrdenamientoExterno.AbrirLectura(archivo))
        {
            OrdenarTramo(lector, mitad, izquierda);
            OrdenarTramo(lector, cantidad - mitad, derecha);
        }
        OrdenamientoExterno.Intercalar(izquierda, derecha, salida);
    }

    private static void OrdenarTramo(BinaryReader lector, int cantidad, string ruta)
    {
        var datos = new int[cantidad];
        for (int i = 0; i < cantidad; i++)
            datos[i] = lector.ReadInt32();
        Array.Sort(datos);
        EscribirArchivo(ruta, datos);
    }

    private static void EscribirArchivo(string ruta, int[] datos)
    {
        using var archivo = OrdenamientoExterno.AbrirEscritura(ruta);
        foreach (int valor in datos)
            archivo.Write(valor);
    }

    private static void ValidarArchivo(string ruta, int[] esperado)
    {
        using var lector = OrdenamientoExterno.AbrirLectura(ruta);
        if (lector.BaseStream.Length != (long)esperado.Length * sizeof(int))
            throw new InvalidDataException("El archivo ordenado tiene una cantidad incorrecta de registros.");

        foreach (int valor in esperado)
            if (lector.ReadInt32() != valor)
                throw new InvalidDataException("El archivo ordenado difiere del resultado esperado.");
    }

    private static double Mediana(double[] muestras)
    {
        Array.Sort(muestras);
        int centro = muestras.Length / 2;
        return muestras.Length % 2 == 0
            ? (muestras[centro - 1] + muestras[centro]) / 2
            : muestras[centro];
    }

    public static void ExportarCsv(string ruta, IEnumerable<Resultado> resultados)
    {
        using var escritor = new StreamWriter(ruta, append: false, new System.Text.UTF8Encoding(true));
        escritor.WriteLine("metodo,tipo,registros,condicion,repeticiones,transcurrido_ms,cpu_ms");
        foreach (var fila in resultados)
        {
            escritor.WriteLine(FormattableString.Invariant(
                $"{fila.Metodo},{fila.Tipo},{fila.Registros},{fila.Condicion},{fila.Repeticiones},{fila.TranscurridoMs:F6},{fila.CpuMs:F6}"));
        }
    }
}
