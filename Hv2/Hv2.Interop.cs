
using System.Runtime.InteropServices;

namespace Hv2UI;

public static partial class Hv2
{
    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
    internal static extern void winmmTimeBeginPeriod(int Period);

    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
    internal static extern void winmmTimeEndPeriod(int Period);

    [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true)]
    public static extern IntPtr k32GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", EntryPoint = "GetConsoleMode")]
    public static extern bool k32GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", EntryPoint = "SetConsoleMode")]
    public static extern bool k32SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
