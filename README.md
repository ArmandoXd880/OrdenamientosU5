# Unidad 5 · Métodos de ordenamiento

Banco de pruebas en C# para siete métodos internos y externos. Compara arreglos aleatorios, ordenados y en orden inverso con 10 000, 50 000 o 100 000 enteros.

## Ejecutar en Windows

1. Instala .NET 8 y la carga **Desarrollo de escritorio de .NET** de Visual Studio 2022.
2. Abre `OrdenamientosU5.sln` y selecciona **WinForms** como proyecto de inicio.
3. Elige **Release** y ejecuta con **Ctrl+F5**.
4. Elige la cantidad de registros, la condición y las repeticiones. Pulsa **Ejecutar**. Con **Todos** y **Todas** se completan 63 casos. Burbuja puede tardar varios segundos por repetición en los casos grandes.
5. Pulsa **Exportar CSV** para guardar las medianas en milisegundos.

También puedes iniciar la aplicación desde PowerShell con el SDK de .NET 8:

```powershell
dotnet run --project WinForms/WinForms.csproj -c Release
```

## Ejecutar el mismo banco por consola

```text
dotnet run --project BancoConsola/BancoConsola.csproj -c Release -- Resultados_U5.csv
```

La consola ejecuta los 63 casos, con tres repeticiones por caso. Cada tamaño usa una semilla fija y los mismos valores para las tres condiciones. Cada salida se verifica contra una referencia ordenada. El CSV distingue el tiempo transcurrido, medido con `Stopwatch`, del tiempo de CPU del proceso. El reloj de CPU de Windows puede redondear pruebas rápidas a cero.

## Código

| Archivo | Contenido |
| --- | --- |
| `Core/OrdenamientoInterno.cs` | Burbuja, Quicksort, Shellsort con saltos de Knuth y Radix LSD para enteros con signo |
| `Core/OrdenamientoExterno.cs` | Intercalación y mezclas directa y natural con archivos binarios temporales |
| `Core/BancoPruebas.cs` | Generación reproducible, cronometraje, verificación y exportación |
| `WinForms/FormularioBanco.cs` | Interfaz de Windows Forms |
| `BancoConsola/Program.cs` | Punto de entrada para reproducir la prueba completa |

En intercalación, la prueba cronometrada incluye preparar y ordenar dos mitades con `Array.Sort` y fusionarlas. Las mezclas trabajan sobre archivos con búferes de 64 KiB. La generación de datos, la clonación o escritura inicial y la verificación quedan fuera del cronometraje.
