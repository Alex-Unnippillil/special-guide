using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpecialGuide.Core.Services;
using Gma.System.MouseKeyHook;
using WinForms = System.Windows.Forms;

namespace SpecialGuide.App;

public partial class App : Application
{
    private IHost? _host;
    private CancellationTokenSource? _cts;
    private WinForms.NotifyIcon? _notifyIcon;
    private IKeyboardMouseEvents? _globalHook;
    private SettingsWindow? _settingsWindow;

    protected override async Task OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
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
                services.AddSingleton<SettingsWindow>();
            })
            .Build();

        await _host.StartAsync(_cts.Token);

        _settingsWindow = _host.Services.GetRequiredService<SettingsWindow>();

        var settings = _host.Services.GetRequiredService<SettingsService>();
        settings.Error += msg => Current.Dispatcher.Invoke(() => MessageBox.Show(msg));

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = new WinForms.ContextMenuStrip()
        };
        _notifyIcon.ContextMenuStrip.Items.Add("Settings", null, (_, _) => ShowSettings());
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (_, _) => Shutdown());
        _notifyIcon.DoubleClick += (_, _) => ShowSettings();

        _globalHook = Hook.GlobalEvents();
        _globalHook.KeyDown += GlobalHook_KeyDown;

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Hide();
    }

    private void GlobalHook_KeyDown(object? sender, WinForms.KeyEventArgs e)
    {
        if (e.Control && e.Alt && e.KeyCode == WinForms.Keys.S)
        {
            ShowSettings();
        }
    }

    private void ShowSettings()
    {
        _settingsWindow?.Show();
        _settingsWindow?.Activate();
    }

    protected override async Task OnExit(ExitEventArgs e)
    {
        _globalHook?.Dispose();
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
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
