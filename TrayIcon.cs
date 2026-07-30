using System.Diagnostics;
using System.Windows.Forms;

namespace TasyGuard;

internal static class TrayIcon
{
    private static NotifyIcon? notifyIcon;

    public static void Initialize()
    {
        notifyIcon = new NotifyIcon
        {
            Text = "TasyGuard",
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            Visible = true
        };

        var menu = new ContextMenuStrip();

        menu.Items.Add("Status", null, (_, _) =>
        {
            MessageBox.Show(
                "TasyGuard está monitorando o Tasy.",
                "TasyGuard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });

        menu.Items.Add("Abrir Log", null, (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Logger.LogPath,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        });

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Sair", null, (_, _) =>
        {
            notifyIcon.Visible = false;

            ProcessWatcher.Stop();

            Application.Exit();
        });

        notifyIcon.ContextMenuStrip = menu;
    }

    public static void NotifyBlocked()
    {
        if (notifyIcon == null)
            return;

        notifyIcon.BalloonTipTitle = "TasyGuard";

        notifyIcon.BalloonTipText =
            "O Tasy Native já está aberto.\nA nova aplicação foi encerrada.";

        notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;

        notifyIcon.ShowBalloonTip(3000);
    }

    public static void NotifyUpdateAvailable(string message)
    {
        if (notifyIcon == null)
            return;

        notifyIcon.BalloonTipTitle = "TasyGuard - Atualização";
        notifyIcon.BalloonTipText = message;
        notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        notifyIcon.ShowBalloonTip(5000);
    }
}