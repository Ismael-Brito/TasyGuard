using System.Diagnostics;
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
