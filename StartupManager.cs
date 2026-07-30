using Microsoft.Win32;

namespace TasyGuard;

internal static class StartupManager
{
    private const string RunRegistryPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void Register()
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    RunRegistryPath,
                    writable: true);

            if (key == null)
            {
                Logger.Write("Não foi possível abrir a chave de inicialização automática.");
                return;
            }

            string path = Environment.ProcessPath ?? string.Empty;
            object? currentValue = key.GetValue("TasyGuard");

            if (currentValue?.ToString() == path)
            {
                Logger.Write("Registro de inicialização automática já estava configurado.");
                return;
            }

            key.SetValue("TasyGuard", path);
            Logger.Write($"Registro de inicialização automática configurado: {path}");
        }
        catch (Exception ex)
        {
            Logger.Write($"Erro ao registrar o aplicativo na inicialização automática: {ex}");
        }
    }
}
