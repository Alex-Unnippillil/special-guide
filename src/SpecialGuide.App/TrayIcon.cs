using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SpecialGuide.App;

public class TrayIcon : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TrayIcon> _logger;
    private NotifyIcon? _notifyIcon;

    public TrayIcon(IServiceProvider services, ILogger<TrayIcon> logger)
    {
        _services = services;
        _logger = logger;
    }

    public void Initialize()
    {
        try
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "SpecialGuide",
                ContextMenuStrip = new ContextMenuStrip()
            };
            _notifyIcon.ContextMenuStrip.Items.Add("Settings…", null, (_, _) => ShowSettings());
            _notifyIcon.ContextMenuStrip.Items.Add("Quit", null, (_, _) => Application.Current.Shutdown());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tray icon");
            _notifyIcon?.Dispose();
            _notifyIcon = null;
        }
    }

    private void ShowSettings()
    {
        try
        {
            var window = _services.GetRequiredService<SettingsWindow>();
            window.Show();
            window.Activate();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open settings window");
        }
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}
