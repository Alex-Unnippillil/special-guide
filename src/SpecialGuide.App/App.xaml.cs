using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpecialGuide.Core.Services;

namespace SpecialGuide.App;

public partial class App : Application
{
    private IHost? _host;
    private CancellationTokenSource? _cts;
    private NotifyIcon? _notifyIcon;

    protected override async Task OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<Overlay.RadialMenuWindow>();
                services.AddSingleton<IRadialMenu>(sp => sp.GetRequiredService<Overlay.RadialMenuWindow>());
                services.AddSingleton<HookService>();
                services.AddSingleton<OverlayService>();
                services.AddSingleton<CaptureService>();
                services.AddHttpClient<OpenAIService>();
                services.AddSingleton<AudioService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<ClipboardService>();
                services.AddSingleton<WindowService>();
                services.AddSingleton<MainWindow>();
                services.AddTransient<SettingsWindow>();
            })
            .Build();

        await _host.StartAsync(_cts.Token);
        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Hide();

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "SpecialGuide",
            ContextMenuStrip = new ContextMenuStrip()
        };
        _notifyIcon.ContextMenuStrip.Items.Add("Settings", null, (_, _) => ShowSettings());
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (_, _) => Shutdown());
    }

    private void ShowSettings()
    {
        if (_host == null) return;
        var window = _host.Services.GetRequiredService<SettingsWindow>();
        window.Show();
        window.Activate();
    }

    protected override async Task OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        if (_host != null && _cts != null)
        {
            var hookService = _host.Services.GetService<HookService>();
            hookService?.Stop();
            await _host.StopAsync(_cts.Token);
            _host.Dispose();
            _cts.Dispose();
        }
        base.OnExit(e);
    }
}
