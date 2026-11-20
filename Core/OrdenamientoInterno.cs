namespace OrdenamientosU5;

public static class OrdenamientoInterno
{
    public static void Burbuja(int[] datos, CancellationToken cancelacion = default)
    {
        for (int ultimo = datos.Length - 1; ultimo > 0; ultimo--)
        {
            cancelacion.ThrowIfCancellationRequested();
            bool huboIntercambio = false;
            for (int i = 0; i < ultimo; i++)
            {
                int actual = datos[i];
                int siguiente = datos[i + 1];
                if (actual <= siguiente)
                    continue;

                datos[i] = siguiente;
                datos[i + 1] = actual;
                huboIntercambio = true;
            }

            if (!huboIntercambio)
                return;
        }
    }

    public static void Quicksort(int[] datos) => Particionar(datos, 0, datos.Length - 1);

    private static void Particionar(int[] datos, int inicio, int fin)
    {
        while (inicio < fin)
        {
            int izquierda = inicio;
            int derecha = fin;
            int pivote = datos[inicio + (fin - inicio) / 2];

            while (izquierda <= derecha)
            {
                while (datos[izquierda] < pivote)
                    izquierda++;

                while (datos[derecha] > pivote)
                    derecha--;

                if (izquierda > derecha)
                    break;

                (datos[izquierda], datos[derecha]) = (datos[derecha], datos[izquierda]);
                izquierda++;
                derecha--;
            }

            // Solo el tramo menor agrega una llamada a la pila.
            if (derecha - inicio < fin - izquierda)
            {
                Particionar(datos, inicio, derecha);
                inicio = izquierda;
            }
            else
            {
                Particionar(datos, izquierda, fin);
                fin = derecha;
            }
        }
    }

    public static void Shellsort(int[] datos)
    {
        int salto = 1;
        while (salto < datos.Length / 3)
            salto = salto * 3 + 1;

        for (; salto > 0; salto /= 3)
        {
            for (int i = salto; i < datos.Length; i++)
            {
                int valor = datos[i];
                int j = i;
                while (j >= salto && datos[j - salto] > valor)
                {
                    datos[j] = datos[j - salto];
                    j -= salto;
                }
                datos[j] = valor;
            }
        }
    }

    public static void Radix(int[] datos)
    {
        if (datos.Length < 2)
            return;

        const int baseRadix = 256;
        int[] origen = datos;
        int[] destino = new int[datos.Length];
        Span<int> conteo = stackalloc int[baseRadix];

        for (int bits = 0; bits < 32; bits += 8)
        {
            conteo.Clear();
            foreach (int valor in origen)
                conteo[Digito(valor, bits)]++;

            int posicion = 0;
            for (int digito = 0; digito < baseRadix; digito++)
            {
                int cantidad = conteo[digito];
                conteo[digito] = posicion;
                posicion += cantidad;
            }

            // En cada pasada se recorre el origen en orden para conservar empates.
            foreach (int valor in origen)
                destino[conteo[Digito(valor, bits)]++] = valor;

            (origen, destino) = (destino, origen);
        }
        // Cuatro pasadas hacen que el resultado termine en el arreglo original.
    }

    private static int Digito(int valor, int bits)
    {
        uint clave = unchecked((uint)valor) ^ 0x80000000u;
        return (int)((clave >> bits) & 255u);
    }
}
