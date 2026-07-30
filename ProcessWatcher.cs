using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace TasyGuard;

internal static class ProcessWatcher
{
    private static ManagementEventWatcher? watcher;
    private static DateTime lastNotificationUtc = DateTime.MinValue;
    private static readonly TimeSpan NotificationDebounce = TimeSpan.FromSeconds(5);

    public static void Start()
    {
        Stop();

        const string query =
            "SELECT * FROM Win32_ProcessStartTrace";

        watcher = new ManagementEventWatcher(
            new WqlEventQuery(query));

        watcher.EventArrived += ProcessoCriado;

        watcher.Start();

        var config = ConfigurationManager.Current;
        Logger.Write($"Monitor de processo iniciado. Query: {query}. Configurações carregadas: {config.Applications.Count}");
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

            // Logger.Write(
            //     $"Evento de processo criado recebido: {nome ?? "[null]"}");

            if (!string.Equals(
                    nome,
                    "TasyNative.exe",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Logger.Write("Evento ignorado: o processo não é TasyNative.exe.");
                return;
            }

            if (!IsMainElectronProcess(e))
            {
                // Logger.Write("Evento ignorado: processo auxiliar do Electron (--type=).");
                return;
            }

            int maxInstances = 1;
            var appConfig = ConfigurationManager.Current.GetApplication(nome!);
            if (appConfig != null)
            {
                maxInstances = Math.Max(1, appConfig.MaxInstances);
                Logger.Write($"Configuração encontrada para {nome}: MaxInstances={maxInstances}");
            }
            else
            {
                Logger.Write($"Nenhuma configuração específica encontrada para {nome}. Usando MaxInstances=1.");
            }

            await Task.Delay(1000);

            VerificarInstancias(maxInstances);
        }
        catch (Exception ex)
        {
            Logger.Write($"Erro no evento de processo: {ex}");
        }
    }

    private static bool IsMainElectronProcess(EventArrivedEventArgs e)
    {
        try
        {
            var processIdValue = e.NewEvent.Properties["ProcessID"]?.Value ??
                e.NewEvent.Properties["ProcessId"]?.Value;

            if (processIdValue == null)
                return true;

            if (!int.TryParse(processIdValue.ToString(), out int pid))
                return true;

            using var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId={pid}");

            foreach (ManagementObject obj in searcher.Get())
            {
                string cmd = obj["CommandLine"]?.ToString() ?? string.Empty;
                Logger.Write($"PID {pid} cmdline: {cmd}");
                return !cmd.Contains("--type=", StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Write($"Falha ao determinar tipo do processo: {ex}");
            return true;
        }
    }

    private static void VerificarInstancias(int maxInstances)
    {
        var principais = TasyProcess.GetMainProcesses();

        Logger.Write(
            $"Verificando instâncias principais de TasyNative.exe. Encontradas: {principais.Count}. MaxInstances={maxInstances}");

        if (principais.Count <= maxInstances)
        {
            Logger.Write("Nenhuma instância extra encontrada.");
            return;
        }

        Logger.Write(
            $"Encontradas {principais.Count} instâncias principais. Mantendo apenas {maxInstances}.");

        var principaisManter = principais.Take(maxInstances).ToList();
        var processosExtras = principais.Skip(maxInstances).ToList();
        var primeira = principaisManter.First();

        foreach (var processo in processosExtras)
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

        if (DateTime.UtcNow - lastNotificationUtc >= NotificationDebounce)
        {
            WindowManager.Activate(primeira);
            TrayIcon.NotifyBlocked();
            lastNotificationUtc = DateTime.UtcNow;
        }
        else
        {
            Logger.Write("Notificação bloqueada para evitar duplicatas.");
        }
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
