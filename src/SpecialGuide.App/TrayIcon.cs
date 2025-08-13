using System;
using System.Windows;
using System.Windows.Forms;

namespace SpecialGuide.App;

public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly MainWindow _window;

    public TrayIcon(MainWindow window)
    {
        _window = window;
        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "SpecialGuide",
            Visible = true
        };

        var menu = new ContextMenuStrip();
        var settingsItem = new ToolStripMenuItem("Settings...");
        settingsItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(settingsItem);

        var quitItem = new ToolStripMenuItem("Quit");
        quitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(quitItem);

        _notifyIcon.ContextMenuStrip = menu;
    }

    private void ShowSettings()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
