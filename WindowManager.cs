using System.Diagnostics;

namespace TasyGuard;

internal static class WindowManager
{
    public static void Activate(Process process)
    {
        try
        {
            if (process.MainWindowHandle == IntPtr.Zero)
                return;

            NativeMethods.ShowWindow(
                process.MainWindowHandle,
                NativeMethods.SW_RESTORE);

            NativeMethods.SetForegroundWindow(
                process.MainWindowHandle);
        }
        catch
        {
        }
    }
}