using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace TasyGuard;

internal sealed class VersionManifest
{
    public string Version { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Force { get; set; }
    public string Message { get; set; } = string.Empty;
}

internal static class UpdateManager
{
    public static void CheckForUpdate()
    {
        var updateSettings = ConfigurationManager.Current.Update;
        if (updateSettings == null || !updateSettings.Enabled)
        {
            Logger.Write("Atualização desativada ou não configurada.");
            return;
        }

        if (string.IsNullOrWhiteSpace(updateSettings.VersionFilePath))
        {
            Logger.Write("Caminho do arquivo de versão não configurado.");
            return;
        }

        try
        {
            if (!File.Exists(updateSettings.VersionFilePath))
            {
                Logger.Write($"Arquivo de versão não encontrado: {updateSettings.VersionFilePath}");
                return;
            }

            string json = File.ReadAllText(updateSettings.VersionFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            VersionManifest? manifest =
                JsonSerializer.Deserialize<VersionManifest>(json, options);

            if (manifest == null)
            {
                Logger.Write($"Arquivo de versão inválido: {updateSettings.VersionFilePath}");
                return;
            }

            ValidateAndApplyUpdate(manifest);
        }
        catch (Exception ex)
        {
            Logger.Write($"Erro ao verificar atualização: {ex}");
        }
    }

    private static void ValidateAndApplyUpdate(VersionManifest manifest)
    {
        string currentVersion = GetCurrentVersion();
        Logger.Write($"Versão atual: {currentVersion}, versão remota: {manifest.Version}");

        if (!IsRemoteVersionNewer(currentVersion, manifest.Version))
        {
            Logger.Write("Nenhuma atualização necessária.");
            return;
        }

        if (string.IsNullOrWhiteSpace(manifest.Url))
        {
            Logger.Write("URL de atualização não informada no manifest.");
            return;
        }

        if (!File.Exists(manifest.Url))
        {
            Logger.Write($"Arquivo de atualização não encontrado: {manifest.Url}");
            return;
        }

        Logger.Write($"Atualização disponível: {manifest.Version}. Forçar = {manifest.Force}");
        string localExe = GetExecutablePath();
        string tempExe = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(localExe) + ".new.exe");

        File.Copy(manifest.Url, tempExe, overwrite: true);
        Logger.Write($"Arquivo de atualização baixado para: {tempExe}");

        if (manifest.Force)
        {
            Logger.Write("Atualização forçada. Preparando substituição do executável.");
            TrayIcon.NotifyUpdateAvailable(manifest.Message ?? "Nova versão disponível. O aplicativo será atualizado.");
            ScheduleSelfUpdate(localExe, tempExe);
        }
        else
        {
            Logger.Write("Atualização opcional detectada. O arquivo foi baixado e será aplicado na próxima execução.");
            TrayIcon.NotifyUpdateAvailable(manifest.Message ?? "Nova versão disponível.");
        }
    }

    private static string GetCurrentVersion()
    {
        try
        {
            string path = GetExecutablePath();
            var info = FileVersionInfo.GetVersionInfo(path);
            return info.FileVersion ?? info.ProductVersion ?? "0.0.0.0";
        }
        catch
        {
            return "0.0.0.0";
        }
    }

    private static string GetExecutablePath()
    {
        return Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, AppDomain.CurrentDomain.FriendlyName);
    }

    private static bool IsRemoteVersionNewer(string current, string remote)
    {
        if (Version.TryParse(current, out Version? currentVersion) &&
            Version.TryParse(remote, out Version? remoteVersion))
        {
            return remoteVersion > currentVersion;
        }

        return !string.Equals(current, remote, StringComparison.OrdinalIgnoreCase);
    }

    private static void ScheduleSelfUpdate(string currentExe, string newExe)
    {
        try
        {
            string updaterPath = Path.Combine(AppContext.BaseDirectory, "TasyGuard.Update.cmd");
            string script = $"@echo off\r\n"
                + "timeout /t 5 /nobreak > nul\r\n"
                + $"move /Y \"{newExe}\" \"{currentExe}\"\r\n"
                + $"start \"\" \"{currentExe}\"\"\r\n"
                + $"del \"%~f0\"\r\n";

            File.WriteAllText(updaterPath, script);
            Logger.Write($"Script de atualização gerado: {updaterPath}");

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C start \"\" \"{updaterPath}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            });

            Logger.Write("Atualizador iniciado. O aplicativo será reiniciado após a atualização.");
            Application.Exit();
        }
        catch (Exception ex)
        {
            Logger.Write($"Falha ao agendar atualização automática: {ex}");
        }
    }
}
