namespace OrdenamientosU5;

public static class OrdenamientoExterno
{
    private const int Buffer = 64 * 1024;

    public static BinaryReader AbrirLectura(string ruta) => new(
        new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, Buffer, FileOptions.SequentialScan));

    public static BinaryWriter AbrirEscritura(string ruta) => new(
        new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None, Buffer, FileOptions.SequentialScan));

    // Ambas entradas deben estar ordenadas. La izquierda tiene prioridad en empates.
    public static void Intercalar(string izquierda, string derecha, string salida)
    {
        using var a = AbrirLectura(izquierda);
        using var b = AbrirLectura(derecha);
        using var destino = AbrirEscritura(salida);
        MezclarTramos(a, Contar(a), b, Contar(b), destino);
    }

    public static void MezclaDirecta(string entrada, string carpeta, int cantidad)
    {
        string izquierda = Path.Combine(carpeta, "izquierda.bin");
        string derecha = Path.Combine(carpeta, "derecha.bin");
        string salida = Path.Combine(carpeta, "salida.bin");
        string actual = entrada;

        for (long ancho = 1; ancho < cantidad; ancho *= 2)
        {
            int tramo = (int)ancho;
            using (var origen = AbrirLectura(actual))
            using (var a = AbrirEscritura(izquierda))
            using (var b = AbrirEscritura(derecha))
            {
                int faltan = cantidad;
                while (faltan > 0)
                {
                    int primero = Math.Min(tramo, faltan);
                    Copiar(origen, a, primero);
                    faltan -= primero;

                    int segundo = Math.Min(tramo, faltan);
                    Copiar(origen, b, segundo);
                    faltan -= segundo;
                }
            }

            using var lectorA = AbrirLectura(izquierda);
            using var lectorB = AbrirLectura(derecha);
            using var destino = AbrirEscritura(salida);
            int restantes = cantidad;
            while (restantes > 0)
            {
                int primero = Math.Min(tramo, restantes);
                restantes -= primero;
                int segundo = Math.Min(tramo, restantes);
                restantes -= segundo;
                MezclarTramos(lectorA, primero, lectorB, segundo, destino);
            }
            actual = salida;
        }

        if (actual == entrada)
            File.Copy(entrada, salida, overwrite: true);
    }

    public static string MezclaNatural(string entrada, string carpeta)
    {
        string izquierda = Path.Combine(carpeta, "izquierda.bin");
        string derecha = Path.Combine(carpeta, "derecha.bin");
        string indice = Path.Combine(carpeta, "tramos.bin");
        string salida = Path.Combine(carpeta, "salida.bin");
        string actual = entrada;

        while (true)
        {
            int tramos = SepararTramosNaturales(actual, izquierda, derecha, indice);
            if (tramos <= 1)
                return actual;

            using var a = AbrirLectura(izquierda);
            using var b = AbrirLectura(derecha);
            using var longitudes = AbrirLectura(indice);
            using var destino = AbrirEscritura(salida);
            for (int i = 0; i < tramos; i += 2)
            {
                int primero = longitudes.ReadInt32();
                int segundo = i + 1 < tramos ? longitudes.ReadInt32() : 0;
                MezclarTramos(a, primero, b, segundo, destino);
            }
            actual = salida;
        }
    }

    private static int SepararTramosNaturales(string ruta, string izquierda, string derecha, string indice)
    {
        using var lector = AbrirLectura(ruta);
        int cantidad = Contar(lector);
        if (cantidad == 0)
            return 0;

        using var a = AbrirEscritura(izquierda);
        using var b = AbrirEscritura(derecha);
        using var longitudes = AbrirEscritura(indice);
        int anterior = lector.ReadInt32();
        int longitud = 1;
        int tramos = 1;
        var destino = a;
        destino.Write(anterior);

        for (int i = 1; i < cantidad; i++)
        {
            int valor = lector.ReadInt32();
            if (valor < anterior)
            {
                longitudes.Write(longitud);
                longitud = 0;
                tramos++;
                destino = destino == a ? b : a;
            }

            destino.Write(valor);
            longitud++;
            anterior = valor;
        }

        longitudes.Write(longitud);
        return tramos;
    }

    private static void MezclarTramos(BinaryReader a, int cantidadA, BinaryReader b, int cantidadB, BinaryWriter salida)
    {
        int valorA = cantidadA > 0 ? a.ReadInt32() : 0;
        int valorB = cantidadB > 0 ? b.ReadInt32() : 0;

        while (cantidadA > 0 && cantidadB > 0)
        {
            if (valorA <= valorB)
            {
                salida.Write(valorA);
                if (--cantidadA > 0)
                    valorA = a.ReadInt32();
            }
            else
            {
                salida.Write(valorB);
                if (--cantidadB > 0)
                    valorB = b.ReadInt32();
            }
        }

        if (cantidadA > 0)
        {
            salida.Write(valorA);
            Copiar(a, salida, cantidadA - 1);
        }
        if (cantidadB > 0)
        {
            salida.Write(valorB);
            Copiar(b, salida, cantidadB - 1);
        }
    }

    private static void Copiar(BinaryReader origen, BinaryWriter destino, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
            destino.Write(origen.ReadInt32());
    }

    private static int Contar(BinaryReader lector) =>
        checked((int)(lector.BaseStream.Length / sizeof(int)));
}
