using System.Diagnostics;
using System.Text;

namespace TasyGuard;

internal static class Logger
{
    private static readonly string LogDir =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TasyGuard");

    private static readonly string LogFile =
        Path.Combine(LogDir, "TasyGuard.log");

    public static void Start()
    {
        try
        {
            Directory.CreateDirectory(LogDir);

            Write("========================================");
            Write("TasyGuard iniciado");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Falha ao inicializar o logger: {ex}");
        }
    }

    public static void Write(string texto)
    {
        try
        {
            File.AppendAllText(
                LogFile,
                $"{DateTime.Now:dd/MM/yyyy HH:mm:ss} - {texto}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Não foi possível gravar no log: {ex}");
        }
    }

    public static void WriteException(Exception ex, string? context = null)
    {
        Write($"{context ?? "Exceção"}: {ex}");
    }

    public static string LogPath => LogFile;
}
