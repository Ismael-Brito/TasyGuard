using System.Threading;
using System.Windows.Forms;

namespace TasyGuard;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        Logger.Start();
        ConfigurationManager.Initialize();
        UpdateManager.CheckForUpdate();
        StartupManager.Register();

        TrayIcon.Initialize();
        ProcessWatcher.Start();

        Logger.Write("Aplicativo iniciado com sucesso.");

        Application.Run(new ApplicationContext());
    }

    private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
    {
        Logger.Write($"Exceção de thread não tratada: {e.Exception}");
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Logger.Write($"Exceção não tratada no domínio: {e.ExceptionObject}");
    }
}
