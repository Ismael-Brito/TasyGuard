using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace TasyGuard;

internal sealed class GuardConfiguration
{
    public List<ApplicationConfiguration> Applications { get; set; } = new();
    public UpdateConfiguration? Update { get; set; }

    public ApplicationConfiguration? GetApplication(string name) =>
        Applications.FirstOrDefault(
            app => string.Equals(app.Name, name, StringComparison.OrdinalIgnoreCase));
}

internal sealed class ApplicationConfiguration
{
    public string Name { get; set; } = string.Empty;
    public int MaxInstances { get; set; } = 1;
}

internal sealed class UpdateConfiguration
{
    public bool Enabled { get; set; }
    public string VersionFilePath { get; set; } = string.Empty;
}

internal static class ConfigurationManager
{
    private const string FileName = "config.json";
    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, FileName);

    private static readonly object syncRoot = new();
    private static FileSystemWatcher? watcher;
    private static System.Threading.Timer? reloadTimer;
    private static GuardConfiguration? current;

    public static GuardConfiguration Current => current ??= Load();

    public static void Initialize()
    {
        Load();
        StartWatcher();
    }

    public static GuardConfiguration Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Logger.Write($"Configuração não encontrada em {ConfigPath}. Usando valores padrão.");
                return current = new GuardConfiguration();
            }

            string json = File.ReadAllText(ConfigPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            GuardConfiguration? config =
                JsonSerializer.Deserialize<GuardConfiguration>(json, options);

            if (config == null)
            {
                Logger.Write($"Configuração inválida em {ConfigPath}. Usando valores padrão.");
                return current = new GuardConfiguration();
            }

            Logger.Write($"Configuração carregada de {ConfigPath}. Aplicações configuradas: {config.Applications.Count}");
            return current = config;
        }
        catch (Exception ex)
        {
            Logger.Write($"Erro ao carregar config.json: {ex}");
            return current = new GuardConfiguration();
        }
    }

    public static GuardConfiguration Reload()
    {
        lock (syncRoot)
        {
            Logger.Write("Recarregando config.json.");
            return Load();
        }
    }

    private static void StartWatcher()
    {
        if (watcher != null)
            return;

        watcher = new FileSystemWatcher(AppContext.BaseDirectory, FileName)
        {
            NotifyFilter = NotifyFilters.FileName |
                           NotifyFilters.LastWrite |
                           NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnConfigFileChanged;
        watcher.Created += OnConfigFileChanged;
        watcher.Renamed += OnConfigFileChanged;
        watcher.Deleted += OnConfigFileChanged;
    }

    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Logger.Write($"Alteração detectada em config.json: {e.ChangeType}. Recarregando em breve.");

        lock (syncRoot)
        {
            reloadTimer ??= new System.Threading.Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);
            reloadTimer.Change(500, Timeout.Infinite);
        }
    }
}
