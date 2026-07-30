# coding: utf-8
from pathlib import Path

files = {
    'Program.cs': '''using System.Threading;
using System.Windows.Forms;

namespace TasyGuard;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.ThreadException += OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        Logger.Start();
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
''',
    'ProcessWatcher.cs': '''using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace TasyGuard;

internal static class ProcessWatcher
{
    private static ManagementEventWatcher? watcher;

    public static void Start()
    {
        Stop();

        const string query =
            "SELECT * FROM Win32_ProcessStartTrace";

        watcher = new ManagementEventWatcher(
            new WqlEventQuery(query));

        watcher.EventArrived += ProcessoCriado;

        watcher.Start();

        Logger.Write($"Monitor de processo iniciado. Query: {query}");
    }

    private static void ProcessoCriado(
        object sender,
        EventArrivedEventArgs e)
    {
        _ = HandleProcessCreatedAsync(e);
    }

    private static async Task HandleProcessCreatedAsync(
        EventArrivedEventArgs e)
    {
        try
        {
            string? nome =
                e.NewEvent.Properties["ProcessName"].Value?.ToString();

            Logger.Write(
                $"Evento de processo criado recebido: {nome ?? "[null]"}");

            if (!string.Equals(
                    nome,
                    "TasyNative.exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                Logger.Write("Evento ignorado: o processo não é TasyNative.exe.");
                return;
            }

            await Task.Delay(1000);

            VerificarInstancias();
        }
        catch (Exception ex)
        {
            Logger.Write($"Erro no evento de processo: {ex}");
        }
    }

    private static void VerificarInstancias()
    {
        var principais = TasyProcess.GetMainProcesses();

        Logger.Write(
            $"Verificando instâncias principais de TasyNative.exe. Encontradas: {principais.Count}");

        if (principais.Count <= 1)
        {
            Logger.Write("Nenhuma instância extra encontrada.");
            return;
        }

        Logger.Write(
            $"Encontradas {principais.Count} instâncias principais.");

        var primeira = principais.First();

        foreach (var processo in principais.Skip(1))
        {
            try
            {
                Logger.Write(
                    $"Encerrando PID {processo.Id} (janela: {processo.MainWindowTitle})");

                if (!processo.CloseMainWindow())
                {
                    Logger.Write($"CloseMainWindow falhou para PID {processo.Id}.");
                }

                if (!processo.WaitForExit(5000))
                {
                    Logger.Write($"Forçando encerramento PID {processo.Id}.");
                    processo.Kill(true);
                }

                Logger.Write($"PID {processo.Id} encerrado.");
            }
            catch (Exception ex)
            {
                Logger.Write($"Erro ao encerrar PID {processo.Id}: {ex}");
            }
        }

        WindowManager.Activate(primeira);

        TrayIcon.NotifyBlocked();
    }

    public static void Stop()
    {
        try
        {
            if (watcher == null)
                return;

            watcher.EventArrived -= ProcessoCriado;
            watcher.Stop();
            watcher.Dispose();
            watcher = null;

            Logger.Write("Monitor de processo parado.");
        }
        catch (Exception ex)
        {
            Logger.Write($"Falha ao parar monitor: {ex}");
        }
    }
}
''',
    'TasyProcess.cs': '''using System.Diagnostics;
using System.Linq;
using System.Management;

namespace TasyGuard;

internal static class TasyProcess
{
    public static List<Process> GetMainProcesses()
    {
        List<Process> lista = new();

        const string query =
            "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='TasyNative.exe'";

        Logger.Write($"Consultando processos principais: {query}");

        using var searcher = new ManagementObjectSearcher(query);

        foreach (ManagementObject obj in searcher.Get())
        {
            string cmd = obj["CommandLine"]?.ToString() ?? string.Empty;

            if (cmd.Contains("--type=", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Write($"Ignorando subprocesso TasyNative.exe: {cmd}");
                continue;
            }

            try
            {
                int pid = Convert.ToInt32(obj["ProcessId"]);
                var process = Process.GetProcessById(pid);
                lista.Add(process);

                Logger.Write($"Processo principal encontrado: PID {pid}, cmdline: {cmd}");
            }
            catch (Exception ex)
            {
                Logger.Write($"Falha ao obter processo TasyNative.exe: {ex.Message}");
            }
        }

        var result = lista
            .OrderBy(p =>
            {
                try
                {
                    return p.StartTime;
                }
                catch
                {
                    return DateTime.MaxValue;
                }
            })
            .ToList();

        Logger.Write($"Total de processos principais retornados: {result.Count}");

        return result;
    }
}
''',
    'Logger.cs': '''using System.Diagnostics;
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
''',
    'StartupManager.cs': '''using Microsoft.Win32;

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
'''
}

for filename, content in files.items():
    Path(filename).write_text(content, encoding='utf-8')

print('files written')
