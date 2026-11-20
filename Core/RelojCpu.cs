using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OrdenamientosU5;

internal static class RelojCpu
{
    private const int RelojCpuProcesoLinux = 2;

    public static double Milisegundos(Process proceso)
    {
        // El reloj de proceso de Linux permite medir trabajos de menos de un milisegundo.
        if (OperatingSystem.IsLinux() && Environment.Is64BitProcess &&
            ClockGetTime(RelojCpuProcesoLinux, out var tiempo) == 0)
        {
            return tiempo.Segundos * 1_000.0 + tiempo.Nanosegundos / 1_000_000.0;
        }

        proceso.Refresh();
        return proceso.TotalProcessorTime.TotalMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TiempoLinux
    {
        public long Segundos;
        public long Nanosegundos;
    }

    [DllImport("libc", EntryPoint = "clock_gettime")]
    private static extern int ClockGetTime(int reloj, out TiempoLinux tiempo);
}
